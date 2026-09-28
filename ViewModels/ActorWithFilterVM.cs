namespace CinemaApp.ViewModels;

public class ActorWithFilterVM
{
    public IEnumerable<Actor> Actors { get; set; } = new List<Actor>();

    public string Query { get; set; } = string.Empty;
    public double TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
