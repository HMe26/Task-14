using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaApp.DataAccess.EntityConfigurations;

public class SeatEntityTypeConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.Property(e => e.SeatNumber).IsRequired().HasMaxLength(10);

        builder.HasIndex(e => new { e.MovieId, e.SeatNumber }).IsUnique();

        builder.HasOne(e => e.Movie)
            .WithMany(e => e.Seats)
            .HasForeignKey(e => e.MovieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
