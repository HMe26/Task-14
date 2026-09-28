namespace CinemaApp.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Canceled
}

public enum PaymentMethod
{
    Stripe,
    Cash
}

public enum PaymentStatus
{
    Pending,
    Succussed,
    Canceled,
    Refunded
}

public class Order : Audit
{
    public int Id { get; set; }
    public decimal TotalPrice { get; set; }
    public OrderStatus OrderStatus { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Stripe;
    public PaymentStatus PaymentStatus { get; set; }
    public string? TransactionId { get; set; }
    public string? SessionId { get; set; }
    public DateTime? PaymentDate { get; set; }
}
