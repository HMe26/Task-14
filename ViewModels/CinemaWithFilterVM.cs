namespace CinemaApp.ViewModels;

public class CinemaWithFilterVM
{
    public IEnumerable<Cinema> Cinemas { get; set; } = new List<Cinema>();

    public string Query { get; set; } = string.Empty;
    public double TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
