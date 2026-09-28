namespace CinemaApp.Models;

public class Audit
{
    public DateTime? CreateAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdateAt { get; set; }
}
