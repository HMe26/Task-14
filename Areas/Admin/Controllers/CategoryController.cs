using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Admin.Controllers;

[Area(AreaConstants.ADMIN_AREA)]
[Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN},{RoleConstants.EMPLOYEE}")]
public class CategoryController : Controller
{
    private readonly IRepository<Category> _repository;
    private readonly IRepository<Movie> _movieRepository;

    public CategoryController(IRepository<Category> repository, IRepository<Movie> movieRepository)
    {
        _repository = repository;
        _movieRepository = movieRepository;
    }

    public IActionResult Index(string? query, int page = 1, int size = 4)
    {
        var categories = _repository.Get();

        if (query is not null)
            categories = categories.Where(e => e.Name.ToLower().Contains(query.ToLower()));

        var totalPages = Math.Ceiling(categories.Count() / (double)size);
        categories = categories.Skip((page - 1) * size).Take(size);

        return View(new CategoryWithFilterVM
        {
            Categories = categories,
            Query = query ?? "",
            TotalPages = totalPages,
            CurrentPage = page,
        });
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Category());
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return View(category);

        await _repository.CreateAsync(category, ct);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not create the category. Please try again.";
            return View(category);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Create Category Successfully";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpGet]
    public IActionResult Update(int id)
    {
        var category = _repository.GetOne(e => e.Id == id, tracked: false);

        if (category is null)
            return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        return View(category);
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Category category, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return View(category);

        _repository.Update(category);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not update the category. Please try again.";
            return View(category);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Update Category Successfully";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var category = _repository.GetOne(e => e.Id == id);

        if (category is null)
            return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        var hasMovies = _movieRepository.Get(e => e.CategoryId == id).Any();
        if (hasMovies)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Cannot delete this category because it still has movies assigned to it. Remove or reassign those movies first.";
            return RedirectToAction(nameof(Index));
        }

        _repository.Delete(category);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not delete the category. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Delete Category Successfully";

        return RedirectToAction(nameof(Index));
    }
}
