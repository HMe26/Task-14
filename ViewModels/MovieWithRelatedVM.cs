namespace CinemaApp.ViewModels;

public class SeatStatusVM
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string RowLabel { get; set; } = string.Empty;
    public int ColumnNumber { get; set; }
    public bool IsBooked { get; set; }
    public bool IsInMyCart { get; set; }
}

public class MovieWithRelatedVM
{
    public Movie Movie { get; set; } = null!;
    public IEnumerable<Actor> Actors { get; set; } = new List<Actor>();
    public IEnumerable<Movie> RelatedMovies { get; set; } = new List<Movie>();
    public IEnumerable<SeatStatusVM> Seats { get; set; } = new List<SeatStatusVM>();
    public bool IsFavorite { get; set; }
    public bool CanBook { get; set; } = true;
}
