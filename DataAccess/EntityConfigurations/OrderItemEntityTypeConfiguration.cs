using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaApp.DataAccess.EntityConfigurations;

public class OrderItemEntityTypeConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ticket history must survive a seat/movie being removed later, and this also
        // avoids a second SQL Server cascade path to OrderItem (the first being via Order).
        builder.HasOne(e => e.Seat)
            .WithMany()
            .HasForeignKey(e => e.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Movie)
            .WithMany()
            .HasForeignKey(e => e.MovieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
