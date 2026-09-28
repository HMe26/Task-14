using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Admin.Controllers;

[Area(AreaConstants.ADMIN_AREA)]
[Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN},{RoleConstants.EMPLOYEE}")]
public class HomeController : Controller
{
    private readonly IRepository<Movie> _movieRepository;
    private readonly IRepository<Cinema> _cinemaRepository;
    private readonly IRepository<Actor> _actorRepository;
    private readonly IRepository<Category> _categoryRepository;

    public HomeController(
        IRepository<Movie> movieRepository,
        IRepository<Cinema> cinemaRepository,
        IRepository<Actor> actorRepository,
        IRepository<Category> categoryRepository)
    {
        _movieRepository = movieRepository;
        _cinemaRepository = cinemaRepository;
        _actorRepository = actorRepository;
        _categoryRepository = categoryRepository;
    }

    public IActionResult Index()
    {
        var latestMovies = _movieRepository
            .Get(includes: [e => e.Category, e => e.Cinema], tracked: false)
            .OrderByDescending(e => e.Id)
            .Take(5)
            .ToList();

        return View(new DashboardVM
        {
            TotalMovies = _movieRepository.Get().Count(),
            TotalCinemas = _cinemaRepository.Get().Count(),
            TotalActors = _actorRepository.Get().Count(),
            TotalCategories = _categoryRepository.Get().Count(),
            LatestMovies = latestMovies,
        });
    }

    public IActionResult NotFoundPage()
    {
        return View();
    }
}
