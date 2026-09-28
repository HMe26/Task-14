using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaApp.Areas.Admin.Controllers;

[Area(AreaConstants.ADMIN_AREA)]
[Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN},{RoleConstants.EMPLOYEE}")]
public class ActorController : Controller
{
    private readonly IFileUpload _fileUpload;
    private readonly IRepository<Actor> _repository;

    public ActorController(IFileUpload fileUpload, IRepository<Actor> repository)
    {
        _fileUpload = fileUpload;
        _repository = repository;
    }

    public IActionResult Index(string? query, int page = 1, int size = 4)
    {
        var actors = _repository.Get();

        if (query is not null)
            actors = actors.Where(e => e.Name.ToLower().Contains(query.ToLower()));

        var totalPages = Math.Ceiling(actors.Count() / (double)size);
        actors = actors.Skip((page - 1) * size).Take(size);

        return View(new ActorWithFilterVM
        {
            Actors = actors,
            Query = query ?? "",
            TotalPages = totalPages,
            CurrentPage = page,
        });
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Actor());
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpPost]
    public async Task<IActionResult> Create(Actor actor, IFormFile? Img, CancellationToken ct = default)
    {
        if (Img is null || Img.Length == 0)
            ModelState.AddModelError(nameof(Img), "Please select an image.");

        if (!ModelState.IsValid)
            return View(actor);

        var fileName = _fileUpload.GenerateFileName(Img!.FileName);

        var filePath = _fileUpload.GenerateFullPath(FileType.Img, "actors", fileName);
        if (filePath is null) return BadRequest();

        _fileUpload.UploadFileLocally(filePath, Img);

        actor.Img = fileName;

        await _repository.CreateAsync(actor, ct);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            _fileUpload.DeleteFileLocally(filePath);
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not create the actor. Please try again.";
            return View(actor);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Create Actor Successfully";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpGet]
    public IActionResult Update(int id)
    {
        var actor = _repository.GetOne(e => e.Id == id, tracked: false);

        if (actor is null)
            return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        return View(actor);
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    [HttpPost]
    public async Task<IActionResult> Update(Actor actor, IFormFile? Img, CancellationToken ct = default)
    {
        var actorInDB = _repository.GetOne(e => e.Id == actor.Id, tracked: false);

        if (actorInDB is null) return NotFound();

        if (!ModelState.IsValid)
        {
            actor.Img = actorInDB.Img;
            return View(actor);
        }

        if (Img is not null && Img.Length > 0)
        {
            var fileName = _fileUpload.GenerateFileName(Img.FileName);

            var filePath = _fileUpload.GenerateFullPath(FileType.Img, "actors", fileName);
            if (filePath is null) return BadRequest();

            _fileUpload.UploadFileLocally(filePath, Img);

            var oldFilePath = _fileUpload.GenerateFullPath(FileType.Img, "actors", actorInDB.Img);
            if (oldFilePath is not null)
                _fileUpload.DeleteFileLocally(oldFilePath);

            actor.Img = fileName;
        }
        else
            actor.Img = actorInDB.Img;

        _repository.Update(actor);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not update the actor. Please try again.";
            return View(actor);
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Update Actor Successfully";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var actor = _repository.GetOne(e => e.Id == id);

        if (actor is null)
            return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

        _repository.Delete(actor);
        var rowsAffected = await _repository.CommitAsync(ct);

        if (rowsAffected == 0)
        {
            TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not delete the actor. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Delete Actor Successfully";

        return RedirectToAction(nameof(Index));
    }
}
