using Microsoft.AspNetCore.Mvc.Rendering;

namespace CinemaApp.ViewModels;

public class MovieWithDetailsVM
{
    public Movie Movie { get; set; } = null!;
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Cinemas { get; set; } = new List<SelectListItem>();
    public IEnumerable<MovieSubImg> MovieSubImgs { get; set; } = new List<MovieSubImg>();

    public IEnumerable<Actor> AllActors { get; set; } = new List<Actor>();
    public IEnumerable<int> SelectedActorIds { get; set; } = new List<int>();
}
