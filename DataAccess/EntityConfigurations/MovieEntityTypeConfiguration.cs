using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaApp.DataAccess.EntityConfigurations;

public class MovieEntityTypeConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Des).IsRequired(false).HasMaxLength(1000);
        builder.Property(e => e.MainImg).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Price).HasPrecision(10, 2);

        builder.HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Cinema)
            .WithMany()
            .HasForeignKey(e => e.CinemaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
