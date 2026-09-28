namespace CinemaApp.Models;

public class Seat
{
    public int Id { get; set; }

    public string SeatNumber { get; set; } = string.Empty;
    public string RowLabel { get; set; } = string.Empty;
    public int ColumnNumber { get; set; }

    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
}
