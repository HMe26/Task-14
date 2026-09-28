namespace CinemaApp.ViewModels;

public class OrderWithFilterVM
{
    public IEnumerable<Order> Orders { get; set; } = new List<Order>();

    public int? Query { get; set; }
    public double TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
