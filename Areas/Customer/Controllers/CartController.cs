using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace CinemaApp.Areas.Customer.Controllers;

[Area(AreaConstants.CUSTOMER_AREA)]
[Authorize]
public class CartController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Cart> _cartRepository;
    private readonly IRepository<Movie> _movieRepository;
    private readonly IRepository<Seat> _seatRepository;
    private readonly IRepository<OrderItem> _orderItemRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ILogger<CartController> _logger;

    public CartController(UserManager<ApplicationUser> userManager,
        IRepository<Cart> cartRepository,
        IRepository<Movie> movieRepository,
        IRepository<Seat> seatRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Order> orderRepository,
        ApplicationDbContext applicationDbContext,
        ILogger<CartController> logger)
    {
        _userManager = userManager;
        _cartRepository = cartRepository;
        _movieRepository = movieRepository;
        _seatRepository = seatRepository;
        _orderItemRepository = orderItemRepository;
        _orderRepository = orderRepository;
        _applicationDbContext = applicationDbContext;
        _logger = logger;
    }

    public async Task<IActionResult> AddToCart(int movieId, int seatId, CancellationToken ct = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var movie = _movieRepository.GetOne(e => e.Id == movieId && e.Status);
        if (movie is null) return NotFound();

        if (movie.DateTime <= DateTime.Now)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "This movie has already started, booking is closed.";
            return RedirectToAction(nameof(HomeController.Details), ControllerConstants.HOME_CONTROLLER, new { id = movieId });
        }

        var seat = _seatRepository.GetOne(e => e.Id == seatId && e.MovieId == movieId);
        if (seat is null) return NotFound();

        // Someone already has a confirmed ticket on this seat
        var isBooked = _orderItemRepository.GetOne(e => e.SeatId == seatId && e.Status == TicketStatus.Confirmed) is not null;
        if (isBooked)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "This seat is already booked. Please choose another one.";
            return RedirectToAction(nameof(HomeController.Details), ControllerConstants.HOME_CONTROLLER, new { id = movieId });
        }

        var alreadyInCart = _cartRepository.GetOne(e => e.SeatId == seatId);
        if (alreadyInCart is not null)
        {
            var message = alreadyInCart.ApplicationUserId == user.Id
                ? "You already added this seat."
                : "This seat is currently held by another customer. Please choose another one.";

            TempData[NotificationConstants.ERROR_NOTIFICATION] = message;
            return RedirectToAction(nameof(HomeController.Details), ControllerConstants.HOME_CONTROLLER, new { id = movieId });
        }

        await _cartRepository.CreateAsync(new Cart
        {
            ApplicationUserId = user.Id,
            MovieId = movieId,
            SeatId = seatId,
            CurrentPrice = movie.Price,
        }, ct);
        await _cartRepository.CommitAsync(ct);

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = $"Seat {seat.SeatNumber} added to your cart";

        return RedirectToAction(nameof(HomeController.Details), ControllerConstants.HOME_CONTROLLER, new { id = movieId });
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var carts = _cartRepository.Get(e => e.ApplicationUserId == user.Id,
            includes: [e => e.Movie, e => e.Seat]).ToList();

        return View(carts);
    }

    public async Task<IActionResult> RemoveSeat(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var cartInDB = _cartRepository.GetOne(e => e.ApplicationUserId == user.Id && e.Id == id);
        if (cartInDB is null) return NotFound();

        _cartRepository.Delete(cartInDB);
        await _cartRepository.CommitAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Pay(CancellationToken ct = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var carts = _cartRepository.Get(e => e.ApplicationUserId == user.Id,
            includes: [e => e.Movie, e => e.Seat]).ToList();

        if (!carts.Any())
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Your cart is empty.";
            return RedirectToAction(nameof(Index));
        }

        Order order = new()
        {
            ApplicationUserId = user.Id,
            OrderStatus = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            TotalPrice = carts.Sum(e => e.CurrentPrice),
        };
        await _orderRepository.CreateAsync(order, ct);
        await _orderRepository.CommitAsync(ct);

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>(),

            Mode = "payment",
            SuccessUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/success?orderId={order.Id}",
            CancelUrl = $"{Request.Scheme}://{Request.Host}/customer/checkout/cancel?orderId={order.Id}",
        };

        foreach (var item in carts)
        {
            options.LineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"{item.Movie.Name} - Seat {item.Seat.SeatNumber}",
                    },
                    UnitAmount = (long)(item.CurrentPrice * 100),
                },
                Quantity = 1,
            });
        }

        var service = new SessionService();
        var session = service.Create(options);
        order.SessionId = session.Id;
        await _orderRepository.CommitAsync(ct);

        TempData["fromPaymentAction"] = Guid.NewGuid();

        return Redirect(session.Url);
    }

    // Confirms the booking now; payment is collected in cash at the counter
    public async Task<IActionResult> PayCash(CancellationToken ct = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var carts = _cartRepository.Get(e => e.ApplicationUserId == user.Id,
            includes: [e => e.Movie, e => e.Seat]).ToList();

        if (!carts.Any())
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Your cart is empty.";
            return RedirectToAction(nameof(Index));
        }

        var transaction = _applicationDbContext.Database.BeginTransaction();
        var skippedSeats = new List<string>();
        int orderId;

        try
        {
            Order order = new()
            {
                ApplicationUserId = user.Id,
                OrderStatus = OrderStatus.Confirmed,
                PaymentMethod = PaymentMethod.Cash,
                PaymentStatus = PaymentStatus.Pending,
                TotalPrice = carts.Sum(e => e.CurrentPrice),
            };
            await _orderRepository.CreateAsync(order, ct);
            await _orderRepository.CommitAsync(ct);
            orderId = order.Id;

            foreach (var item in carts)
            {
                // Someone might have booked this seat while it was sitting in the cart
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
                }, ct);
            }
            await _orderItemRepository.CommitAsync(ct);

            foreach (var item in carts)
                _cartRepository.Delete(item);
            await _cartRepository.CommitAsync(ct);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            transaction.Rollback();

            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not complete the booking. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        if (skippedSeats.Any())
            TempData[NotificationConstants.ERROR_NOTIFICATION] = $"Seat(s) {string.Join(", ", skippedSeats)} got booked by someone else just before checkout. Please contact support.";

        TempData["fromPaymentAction"] = Guid.NewGuid();

        return RedirectToAction(nameof(CheckoutController.Cash), ControllerConstants.CHECKOUT_CONTROLLER, new { area = AreaConstants.CUSTOMER_AREA, orderId });
    }
}
