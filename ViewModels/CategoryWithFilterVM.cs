namespace CinemaApp.ViewModels;

public class CategoryWithFilterVM
{
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();

    public string Query { get; set; } = string.Empty;
    public double TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
