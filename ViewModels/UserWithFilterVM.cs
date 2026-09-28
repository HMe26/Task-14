namespace CinemaApp.ViewModels;

public class UserRowVM
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UserWithFilterVM
{
    public IEnumerable<UserRowVM> Users { get; set; } = new List<UserRowVM>();

    public string Query { get; set; } = string.Empty;
    public double TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
