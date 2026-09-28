using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaApp.DataAccess.EntityConfigurations;

public class CartEntityTypeConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.Property(e => e.CurrentPrice).HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.ApplicationUser)
            .WithMany()
            .HasForeignKey(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Seat)
            .WithMany()
            .HasForeignKey(e => e.SeatId)
            .OnDelete(DeleteBehavior.Cascade);

        // Movie is already reachable via Seat -> Movie (cascade), so this FK must not
        // also cascade, otherwise SQL Server rejects it as a second cascade path to Cart.
        builder.HasOne(e => e.Movie)
            .WithMany()
            .HasForeignKey(e => e.MovieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
