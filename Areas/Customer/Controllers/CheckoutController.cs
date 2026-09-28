using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace CinemaApp.Areas.Customer.Controllers;

[Area(AreaConstants.CUSTOMER_AREA)]
[Authorize]
public class CheckoutController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(UserManager<ApplicationUser> userManager,
        IRepository<Cart> cartRepository,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        ApplicationDbContext applicationDbContext,
        ILogger<CheckoutController> logger)
    {
        _userManager = userManager;
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _applicationDbContext = applicationDbContext;
        _logger = logger;
    }

    public async Task<IActionResult> Success(int orderId)
    {
        if (TempData["fromPaymentAction"] is null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var transaction = _applicationDbContext.Database.BeginTransaction();
        var skippedSeats = new List<string>();

        try
        {
            // 1. Update order status
            var order = _orderRepository.GetOne(e => e.Id == orderId && e.ApplicationUserId == user.Id);
            if (order is null || order.OrderStatus == OrderStatus.Confirmed) return NotFound();

            var service = new SessionService();
            var session = service.Get(order.SessionId);

            order.PaymentDate = DateTime.Now;
            order.PaymentStatus = PaymentStatus.Succussed;
            order.OrderStatus = OrderStatus.Confirmed;
            order.TransactionId = session.PaymentIntentId;

            await _orderRepository.CommitAsync();

            // 2. Move each held seat to a confirmed ticket (re-checking nobody booked it meanwhile)
            var carts = _cartRepository.Get(e => e.ApplicationUserId == user.Id,
                includes: [e => e.Movie, e => e.Seat]).ToList();

            foreach (var item in carts)
            {
                var seatAlreadyTaken = _orderItemRepository.GetOne(e => e.SeatId == item.SeatId && e.Status == TicketStatus.Confirmed) is not null;

                if (seatAlreadyTaken)
                {
                    skippedSeats.Add(item.Seat.SeatNumber);
                    continue;
                }

                await _orderItemRepository.CreateAsync(new()
                {
                    OrderId = orderId,
                    MovieId = item.MovieId,
                    SeatId = item.SeatId,
                    Price = item.CurrentPrice,
                    Status = TicketStatus.Confirmed,
                });
            }
            await _orderItemRepository.CommitAsync();

            // 3. clear the cart
            foreach (var item in carts)
                _cartRepository.Delete(item);
            await _cartRepository.CommitAsync();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            transaction.Rollback();
        }

        if (skippedSeats.Any())
            TempData[NotificationConstants.ERROR_NOTIFICATION] = $"Seat(s) {string.Join(", ", skippedSeats)} got booked by someone else just before your payment. You will be refunded for those - please contact support.";

        ViewData["OrderId"] = orderId;

        return View();
    }

    public IActionResult Cancel(int? orderId)
    {
        if (TempData["fromPaymentAction"] is null) return NotFound();

        ViewData["OrderId"] = orderId;

        return View();
    }

    public async Task<IActionResult> Cash(int orderId)
    {
        if (TempData["fromPaymentAction"] is null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var order = _orderRepository.GetOne(e => e.Id == orderId && e.ApplicationUserId == user.Id);
        if (order is null || order.PaymentMethod != PaymentMethod.Cash) return NotFound();

        return View(order);
    }
}
