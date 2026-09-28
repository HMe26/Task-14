namespace CinemaApp.ViewModels;

public class MovieWithFilterVM
{
    public IEnumerable<Movie> Movies { get; set; } = new List<Movie>();
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    public IEnumerable<Cinema> Cinemas { get; set; } = new List<Cinema>();

    public string Name { get; set; } = string.Empty;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? CategoryId { get; set; }
    public int? CinemaId { get; set; }
    public double TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
