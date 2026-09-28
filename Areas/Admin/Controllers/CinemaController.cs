using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Admin.Controllers;

[Area(AreaConstants.ADMIN_AREA)]
[Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN},{RoleConstants.EMPLOYEE}")]
public class CinemaController : Controller
{
    private readonly IFileUpload _fileUpload;
    private readonly IRepository<Cinema> _repository;
    private readonly IRepository<Movie> _movieRepository;

    public CinemaController(IFileUpload fileUpload, IRepository<Cinema> repository, IRepository<Movie> movieRepository)
    {
        _fileUpload = fileUpload;
        _repository = repository;
        _movieRepository = movieRepository;
    }

    public IActionResult Index(string? query, int page = 1, int size = 4)
    {
        var cinemas = _repository.Get();

        if (query is not null)
            cinemas = cinemas.Where(e => e.Name.ToLower().Contains(query.ToLower()));

        var totalPages = Math.Ceiling(cinemas.Count() / (double)size);
        cinemas = cinemas.Skip((page - 1) * size).Take(size);

        return View(new CinemaWithFilterVM
        {
            Cinemas = cinemas,
            Query = query ?? "",
            TotalPages = totalPages,
            CurrentPage = page,
        });
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Cinema());
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpPost]
    public async Task<IActionResult> Create(Cinema cinema, IFormFile? Img, CancellationToken ct = default)
    {
        if (Img is null || Img.Length == 0)
            ModelState.AddModelError(nameof(Img), "Please select an image.");

        if (!ModelState.IsValid)
            return View(cinema);

        var fileName = _fileUpload.GenerateFileName(Img!.FileName);

        var filePath = _fileUpload.GenerateFullPath(FileType.Img, "cinemas", fileName);
        if (filePath is null) return BadRequest();

        _fileUpload.UploadFileLocally(filePath, Img);

        cinema.Img = fileName;

        await _repository.CreateAsync(cinema, ct);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            _fileUpload.DeleteFileLocally(filePath);
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not create the cinema. Please try again.";
            return View(cinema);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Create Cinema Successfully";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpGet]
    public IActionResult Update(int id)
    {
        var cinema = _repository.GetOne(e => e.Id == id, tracked: false);

        if (cinema is null)
            return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        return View(cinema);
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpPost]
    public async Task<IActionResult> Update(Cinema cinema, IFormFile? Img, CancellationToken ct = default)
    {
        var cinemaInDB = _repository.GetOne(e => e.Id == cinema.Id, tracked: false);

        if (cinemaInDB is null) return NotFound();

        if (!ModelState.IsValid)
        {
            cinema.Img = cinemaInDB.Img;
            return View(cinema);
        }

        if (Img is not null && Img.Length > 0)
        {
            var fileName = _fileUpload.GenerateFileName(Img.FileName);

            var filePath = _fileUpload.GenerateFullPath(FileType.Img, "cinemas", fileName);
            if (filePath is null) return BadRequest();

            _fileUpload.UploadFileLocally(filePath, Img);

            var oldFilePath = _fileUpload.GenerateFullPath(FileType.Img, "cinemas", cinemaInDB.Img);
            if (oldFilePath is not null)
                _fileUpload.DeleteFileLocally(oldFilePath);

            cinema.Img = fileName;
        }
        else
            cinema.Img = cinemaInDB.Img;

        _repository.Update(cinema);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not update the cinema. Please try again.";
            return View(cinema);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Update Cinema Successfully";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var cinema = _repository.GetOne(e => e.Id == id);

        if (cinema is null)
            return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        var hasMovies = _movieRepository.Get(e => e.CinemaId == id).Any();
        if (hasMovies)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Cannot delete this cinema because it still has movies assigned to it. Remove or reassign those movies first.";
            return RedirectToAction(nameof(Index));
        }

        _repository.Delete(cinema);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not delete the cinema. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Delete Cinema Successfully";

        return RedirectToAction(nameof(Index));
    }
}
