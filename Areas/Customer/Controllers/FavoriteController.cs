using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Customer.Controllers;

[Area(AreaConstants.CUSTOMER_AREA)]
[Authorize]
public class FavoriteController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<Favorite> _favoriteRepository;
    private readonly IRepository<Movie> _movieRepository;

    public FavoriteController(UserManager<ApplicationUser> userManager,
        IRepository<Favorite> favoriteRepository,
        IRepository<Movie> movieRepository)
    {
        _userManager = userManager;
        _favoriteRepository = favoriteRepository;
        _movieRepository = movieRepository;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var movieIds = _favoriteRepository.Get(e => e.ApplicationUserId == user.Id).Select(e => e.MovieId).ToList();
        var movies = _movieRepository.Get(e => movieIds.Contains(e.Id), includes: [e => e.Category, e => e.Cinema]).ToList();

        return View(movies);
    }

    public async Task<IActionResult> Toggle(int movieId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var existing = _favoriteRepository.GetOne(e => e.ApplicationUserId == user.Id && e.MovieId == movieId);

        if (existing is not null)
        {
            _favoriteRepository.Delete(existing);
            TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Removed from favorites";
        }
        else
        {
            await _favoriteRepository.CreateAsync(new()
            {
                ApplicationUserId = user.Id,
                MovieId = movieId,
            });
            TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Added to favorites";
        }

        await _favoriteRepository.CommitAsync();

        return RedirectToAction(nameof(HomeController.Details), ControllerConstants.HOME_CONTROLLER, new { id = movieId });
    }
}
