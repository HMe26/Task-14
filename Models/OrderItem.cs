namespace CinemaApp.Models;

public enum TicketStatus
{
    Confirmed,
    Canceled
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public decimal Price { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Confirmed;
}
