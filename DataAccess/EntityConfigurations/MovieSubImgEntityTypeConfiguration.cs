using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaApp.DataAccess.EntityConfigurations;

public class MovieSubImgEntityTypeConfiguration : IEntityTypeConfiguration<MovieSubImg>
{
    public void Configure(EntityTypeBuilder<MovieSubImg> builder)
    {
        builder.Property(e => e.SubImg).IsRequired().HasMaxLength(500);
    }
}
