using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CinemaApp.Areas.Admin.Controllers;

[Area(AreaConstants.ADMIN_AREA)]
[Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN},{RoleConstants.EMPLOYEE}")]
public class MovieController : Controller
{
  private readonly IFileUpload _fileUpload;

  private readonly IRepository<Movie> _movieRepository;
  private readonly IBulkRepository<MovieSubImg> _movieSubImgRepository;
  private readonly IBulkRepository<MovieActor> _movieActorRepository;
  private readonly IRepository<Category> _categoryRepository;
  private readonly IRepository<Cinema> _cinemaRepository;
  private readonly IRepository<Actor> _actorRepository;
  private readonly IBulkRepository<Seat> _seatRepository;

  public MovieController(
      IFileUpload fileUpload,
      IRepository<Movie> movieRepository,
      IBulkRepository<MovieSubImg> movieSubImgRepository,
      IBulkRepository<MovieActor> movieActorRepository,
      IRepository<Category> categoryRepository,
      IRepository<Cinema> cinemaRepository,
      IRepository<Actor> actorRepository,
      IBulkRepository<Seat> seatRepository)
  {
    _fileUpload = fileUpload;
    _movieRepository = movieRepository;
    _movieSubImgRepository = movieSubImgRepository;
    _movieActorRepository = movieActorRepository;
    _categoryRepository = categoryRepository;
    _cinemaRepository = cinemaRepository;
    _actorRepository = actorRepository;
    _seatRepository = seatRepository;
  }

  public IActionResult Index(MovieFilterVM movieFilterVM, int page = 1, int size = 5)
  {
    var movies = _movieRepository.Get(includes: [e => e.Category, e => e.Cinema]);

    // Filter

    if (movieFilterVM.name is not null)
      movies = movies.Where(e => e.Name.ToLower().Contains(movieFilterVM.name.ToLower()));

    if (movieFilterVM.minPrice is not null)
      movies = movies.Where(e => e.Price >= movieFilterVM.minPrice);

    if (movieFilterVM.maxPrice is not null)
      movies = movies.Where(e => e.Price < movieFilterVM.maxPrice);

    if (movieFilterVM.categoryId is not null)
      movies = movies.Where(e => e.CategoryId == movieFilterVM.categoryId);

    if (movieFilterVM.cinemaId is not null)
      movies = movies.Where(e => e.CinemaId == movieFilterVM.cinemaId);

    // Pagination

    var totalPages = Math.Ceiling(movies.Count() / (double)size);
    movies = movies.Skip((page - 1) * size).Take(size);

    var categories = _categoryRepository.Get().ToList();
    var cinemas = _cinemaRepository.Get().ToList();

    return View(new MovieWithFilterVM()
    {
      Movies = movies.ToList(),
      Categories = categories,
      Cinemas = cinemas,
      TotalPages = totalPages,
      CurrentPage = page,
      Name = movieFilterVM.name ?? "",
      MinPrice = movieFilterVM.minPrice,
      MaxPrice = movieFilterVM.maxPrice,
      CategoryId = movieFilterVM.categoryId,
      CinemaId = movieFilterVM.cinemaId,
    });
  }


  private MovieWithDetailsVM BuildDetailsVM(Movie movie, IEnumerable<MovieSubImg>? movieSubImgs = null, IEnumerable<int>? selectedActorIds = null)
  {
    return new MovieWithDetailsVM()
    {
      Movie = movie,
      MovieSubImgs = movieSubImgs?.ToList() ?? new List<MovieSubImg>(),
      Categories = _categoryRepository.Get().Select(e => new SelectListItem
      {
        Text = e.Name,
        Value = e.Id.ToString(),
        Selected = e.Id == movie.CategoryId,
      }).ToList(),
      Cinemas = _cinemaRepository.Get().Select(e => new SelectListItem
      {
        Text = e.Name,
        Value = e.Id.ToString(),
        Selected = e.Id == movie.CinemaId,
      }).ToList(),
      AllActors = _actorRepository.Get().ToList(),
      SelectedActorIds = selectedActorIds?.ToList() ?? new List<int>(),
    };
  }


  private static readonly TimeSpan MinimumCinemaGap = TimeSpan.FromHours(2);


  private static DateTime? BuildShowDateTime(string? movieDate, int hour, int minute, string? amPm)
  {
    if (string.IsNullOrWhiteSpace(movieDate))
      return null;

    if (!DateOnly.TryParse(movieDate, out var date))
      return null;

    var hour24 = amPm == "PM"
        ? (hour % 12) + 12
        : (hour % 12);

    return date.ToDateTime(new TimeOnly(hour24, minute));
  }


  private bool HasCinemaSchedulingConflict(int cinemaId, DateTime showDateTime, int excludeMovieId)
  {
    return _movieRepository
        .Get(m => m.CinemaId == cinemaId && m.Id != excludeMovieId)
        .Select(m => m.DateTime)
        .ToList()
        .Any(existing => (existing - showDateTime).Duration() < MinimumCinemaGap);
  }

  [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
  [HttpGet]
  public IActionResult Create()
  {
    return View(BuildDetailsVM(new Movie()));
  }

  [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
  [HttpPost]
  public async Task<IActionResult> Create(Movie movie, string? movieDate, int hour, int minute, string? amPm, IFormFile? mainImg, List<IFormFile>? subImgs, List<int>? actorIds, CancellationToken ct = default)
  {
    subImgs ??= new List<IFormFile>();
    actorIds ??= new List<int>();

    if (mainImg is null || mainImg.Length == 0)
      ModelState.AddModelError(nameof(mainImg), "Please select a main image.");

    var showDateTime = BuildShowDateTime(movieDate, hour, minute, amPm);

    if (showDateTime is null)
      ModelState.AddModelError(nameof(movieDate), "Please choose a date and time.");
    else if (showDateTime.Value.Date < DateTime.Today)
      ModelState.AddModelError(nameof(movieDate), "The date can't be in the past.");
    else if (HasCinemaSchedulingConflict(movie.CinemaId, showDateTime.Value, movie.Id))
      ModelState.AddModelError(nameof(movieDate), $"This cinema already has a movie scheduled within {MinimumCinemaGap.TotalHours:0} hours of this time. Please choose a different time.");

    if (!ModelState.IsValid)
      return View(BuildDetailsVM(movie, selectedActorIds: actorIds));

    movie.DateTime = showDateTime!.Value;

    var fileName = _fileUpload.GenerateFileName(mainImg!.FileName);

    var filePath = _fileUpload.GenerateFullPath(FileType.Img, "movies", fileName);
    if (filePath is null) return BadRequest();

    _fileUpload.UploadFileLocally(filePath, mainImg);

    movie.MainImg = fileName;

    await _movieRepository.CreateAsync(movie, ct);
    var rowsAffected = await _movieRepository.CommitAsync(ct);

    if (rowsAffected == 0)
    {
      _fileUpload.DeleteFileLocally(filePath);
      TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not create the movie. Please try again.";
      return View(BuildDetailsVM(movie, selectedActorIds: actorIds));
    }

    if (subImgs.Any())
    {
      foreach (var item in subImgs)
      {
        var subFileName = _fileUpload.GenerateFileName(item.FileName);
        if (subFileName is null) return BadRequest();

        var subFilePath = _fileUpload.GenerateFullPath(FileType.Img, "movies\\sub-imgs", subFileName);
        if (subFilePath is null) return BadRequest();

        _fileUpload.UploadFileLocally(subFilePath, item);

        await _movieSubImgRepository.CreateAsync(new()
        {
          SubImg = subFileName,
          MovieId = movie.Id
        });
      }
    }

    // Generate the seat map for this movie's showing
    await _seatRepository.CreateRangeAsync(SeatMapGenerator.Generate(movie.Id, movie.TotalSeats), ct);

    if (actorIds.Any())
    {
      foreach (var actorId in actorIds)
      {
        await _movieActorRepository.CreateAsync(new()
        {
          MovieId = movie.Id,
          ActorId = actorId
        });
      }
    }

    await _movieRepository.CommitAsync(ct);


    TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Create Movie Successfully";

    return RedirectToAction(nameof(Index));
  }

  [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
  [HttpGet]
  public IActionResult Update(int id)
  {
    var movie = _movieRepository.GetOne(e => e.Id == id, tracked: false);

    if (movie is null) return NotFound();

    var movieSubImgs = _movieSubImgRepository.Get(e => e.MovieId == movie.Id).ToList();
    var selectedActorIds = _movieActorRepository.Get(e => e.MovieId == movie.Id).Select(e => e.ActorId).ToList();

    return View(BuildDetailsVM(movie, movieSubImgs, selectedActorIds));
  }

  [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
  [HttpPost]
  public async Task<IActionResult> Update(Movie movie, string? movieDate, int hour, int minute, string? amPm, IFormFile? mainImg, List<IFormFile>? subImgs, List<int>? deleteSubImgIds, List<int>? actorIds, CancellationToken ct = default)
  {
    subImgs ??= new List<IFormFile>();
    deleteSubImgIds ??= new List<int>();
    actorIds ??= new List<int>();

    var movieInDB = _movieRepository.GetOne(e => e.Id == movie.Id, tracked: false);
    if (movieInDB is null) return NotFound();

    var showDateTime = BuildShowDateTime(movieDate, hour, minute, amPm);

    if (showDateTime is null)
      ModelState.AddModelError(nameof(movieDate), "Please choose a date and time.");
    else if (showDateTime.Value.Date < DateTime.Today)
      ModelState.AddModelError(nameof(movieDate), "The date can't be in the past.");
    else if (HasCinemaSchedulingConflict(movie.CinemaId, showDateTime.Value, movie.Id))
      ModelState.AddModelError(nameof(movieDate), $"This cinema already has a movie scheduled within {MinimumCinemaGap.TotalHours:0} hours of this time. Please choose a different time.");

    if (!ModelState.IsValid)
    {
      var currentSubImgs = _movieSubImgRepository.Get(e => e.MovieId == movie.Id).ToList();
      movie.MainImg = movieInDB.MainImg;
      movie.DateTime = showDateTime ?? movieInDB.DateTime;
      return View(BuildDetailsVM(movie, currentSubImgs, actorIds));
    }

    movie.DateTime = showDateTime!.Value;

    if (mainImg is not null && mainImg.Length > 0)
    {
      // create new img
      var fileName = _fileUpload.GenerateFileName(mainImg.FileName);
      if (fileName is null) return BadRequest();

      var filePath = _fileUpload.GenerateFullPath(FileType.Img, "movies", fileName);
      if (filePath is null) return BadRequest();

      _fileUpload.UploadFileLocally(filePath, mainImg);

      // delete old img from wwwroot
      var oldFilePath = _fileUpload.GenerateFullPath(FileType.Img, "movies", movieInDB.MainImg);
      if (oldFilePath is null) return BadRequest();

      _fileUpload.DeleteFileLocally(oldFilePath);

      movie.MainImg = fileName;
    }
    else
      movie.MainImg = movieInDB.MainImg;

    _movieRepository.Update(movie);
    var rowsAffected = await _movieRepository.CommitAsync(ct);

    if (rowsAffected == 0)
    {
      TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not update the movie. Please try again.";
      var currentSubImgs = _movieSubImgRepository.Get(e => e.MovieId == movie.Id).ToList();
      return View(BuildDetailsVM(movie, currentSubImgs, actorIds));
    }

    if (subImgs.Any() || deleteSubImgIds.Any())
    {
      if (deleteSubImgIds.Any())
      {
        var imgsToDelete = _movieSubImgRepository
            .Get(e => e.MovieId == movie.Id && deleteSubImgIds.Contains(e.Id))
            .ToList();

        foreach (var item in imgsToDelete)
        {
          var oldFilePath = _fileUpload.GenerateFullPath(FileType.Img, "movies\\sub-imgs", item.SubImg);
          if (oldFilePath is not null)
            _fileUpload.DeleteFileLocally(oldFilePath);
        }

        _movieSubImgRepository.DeleteRange(imgsToDelete);
      }

      foreach (var item in subImgs)
      {
        var fileName = _fileUpload.GenerateFileName(item.FileName);
        if (fileName is null) return BadRequest();

        var filePath = _fileUpload.GenerateFullPath(FileType.Img, "movies\\sub-imgs", fileName);
        if (filePath is null) return BadRequest();

        _fileUpload.UploadFileLocally(filePath, item);

        await _movieSubImgRepository.CreateAsync(new()
        {
          SubImg = fileName,
          MovieId = movie.Id
        });
      }
    }

    var oldActorLinks = _movieActorRepository.Get(e => e.MovieId == movie.Id);
    _movieActorRepository.DeleteRange(oldActorLinks);

    if (actorIds.Any())
    {
      foreach (var actorId in actorIds)
      {
        await _movieActorRepository.CreateAsync(new()
        {
          MovieId = movie.Id,
          ActorId = actorId
        });
      }
    }

    await _movieRepository.CommitAsync(ct);

    TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Update Movie Successfully";

    return RedirectToAction(nameof(Index));
  }

  [Authorize(Roles = $"{RoleConstants.SUPER_ADMIN},{RoleConstants.ADMIN}")]
  public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
  {
    var movie = _movieRepository.GetOne(e => e.Id == id);

    if (movie is null)
      return RedirectToAction(nameof(HomeController.NotFoundPage), ControllerConstants.HOME_CONTROLLER);

    var subImgs = _movieSubImgRepository.Get(e => e.MovieId == movie.Id);
    foreach (var item in subImgs)
    {
      var filePath = _fileUpload.GenerateFullPath(FileType.Img, "movies\\sub-imgs", item.SubImg);
      if (filePath is not null)
        _fileUpload.DeleteFileLocally(filePath);
    }
    _movieSubImgRepository.DeleteRange(subImgs);

    var mainImgPath = _fileUpload.GenerateFullPath(FileType.Img, "movies", movie.MainImg);
    if (mainImgPath is not null)
      _fileUpload.DeleteFileLocally(mainImgPath);

    var actorLinks = _movieActorRepository.Get(e => e.MovieId == movie.Id);
    _movieActorRepository.DeleteRange(actorLinks);

    _movieRepository.Delete(movie);
    var rowsAffected = await _movieRepository.CommitAsync(ct);

    if (rowsAffected == 0)
    {
      TempData[NotificationConstants.ERROR_NOTIFICATION] = "Could not delete the movie. Please try again.";
      return RedirectToAction(nameof(Index));
    }

    TempData[NotificationConstants.SUCCESS_NOTIFICATION] = "Delete Movie Successfully";

    return RedirectToAction(nameof(Index));
  }
}
