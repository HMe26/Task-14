using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Customer.Controllers;

[Area(AreaConstants.CUSTOMER_AREA)]
[Authorize]
public class OrderController : Controller
{
    private static readonly TimeSpan CancellationWindow = TimeSpan.FromHours(24);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;

    public OrderController(UserManager<ApplicationUser> userManager,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository)
    {
        _userManager = userManager;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
    }

    public async Task<IActionResult> Index(int? query, int page = 1, int size = 5)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var orders = _orderRepository.Get(e => e.ApplicationUserId == user.Id && e.OrderStatus == OrderStatus.Confirmed);

        if (query is not null)
            orders = orders.Where(e => e.Id == query);

        var totalPages = Math.Ceiling(orders.Count() / (double)size);
        orders = orders.Skip((page - 1) * size).Take(size);

        return View(new OrderWithFilterVM
        {
            Orders = orders.ToList(),
            Query = query,
            TotalPages = totalPages,
            CurrentPage = page,
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var order = _orderRepository.GetOne(e => e.Id == id && e.ApplicationUserId == user.Id);
        if (order is null) return NotFound();

        var orderItems = _orderItemRepository
            .Get(e => e.OrderId == id, includes: [e => e.Movie, e => e.Seat])
            .ToList();

        return View(orderItems);
    }

    public async Task<IActionResult> CancelTicket(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var orderItem = _orderItemRepository.GetOne(e => e.Id == id, includes: [e => e.Order, e => e.Movie, e => e.Seat]);
        if (orderItem is null || orderItem.Order.ApplicationUserId != user.Id) return NotFound();

        if (orderItem.Status == TicketStatus.Canceled)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "This ticket is already canceled.";
            return RedirectToAction(nameof(Details), new { id = orderItem.OrderId });
        }

        if (orderItem.Movie.DateTime - DateTime.Now < CancellationWindow)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Tickets can only be canceled up to 24 hours before the movie starts.";
            return RedirectToAction(nameof(Details), new { id = orderItem.OrderId });
        }

        orderItem.Status = TicketStatus.Canceled;
        await _orderItemRepository.CommitAsync();

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = $"Seat {orderItem.Seat.SeatNumber} canceled successfully";

        return RedirectToAction(nameof(Details), new { id = orderItem.OrderId });
    }
}
