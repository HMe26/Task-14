using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Admin.Controllers;

[Area(AreaConstants.ADMIN_AREA)]
[Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
public class OrderController : Controller
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;

    public OrderController(IRepository<Order> orderRepository, IRepository<OrderItem> orderItemRepository)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
    }

    public IActionResult Index(int? query, int page = 1, int size = 10)
    {
        var orders = _orderRepository.Get(e => e.OrderStatus == OrderStatus.Confirmed, includes: [e => e.ApplicationUser]);

        if (query is not null)
            orders = orders.Where(e => e.Id == query);

        var totalPages = Math.Ceiling(orders.Count() / (double)size);
        orders = orders.OrderByDescending(e => e.PaymentDate).Skip((page - 1) * size).Take(size);

        return View(new OrderWithFilterVM
        {
            Orders = orders.ToList(),
            Query = query,
            TotalPages = totalPages,
            CurrentPage = page,
        });
    }

    public IActionResult Details(int id)
    {
        var order = _orderRepository.GetOne(e => e.Id == id, includes: [e => e.ApplicationUser]);
        if (order is null) return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        var orderItems = _orderItemRepository.Get(e => e.OrderId == id, includes: [e => e.Movie, e => e.Seat]).ToList();

        ViewData["Order"] = order;

        return View(orderItems);
    }

    // Staff confirm here once the cash has actually been collected at the counter
    [HttpPost]
    public IActionResult MarkAsPaid(int id)
    {
        var order = _orderRepository.GetOne(e => e.Id == id);
        if (order is null) return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        if (order.PaymentMethod != PaymentMethod.Cash)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Only cash orders can be confirmed manually.";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.PaymentStatus = PaymentStatus.Succussed;
        order.PaymentDate = DateTime.Now;
        _orderRepository.Update(order);
        _orderRepository.CommitAsync().GetAwaiter().GetResult();

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = $"Order #{id} marked as paid";

        return RedirectToAction(nameof(Details), new { id });
    }
}
