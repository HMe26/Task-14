namespace CinemaApp.ViewModels;

public class DashboardVM
{
    public int TotalMovies { get; set; }
    public int TotalCinemas { get; set; }
    public int TotalActors { get; set; }
    public int TotalCategories { get; set; }
    public IEnumerable<Movie> LatestMovies { get; set; } = new List<Movie>();
}
