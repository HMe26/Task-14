using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaApp.DataAccess.EntityConfigurations;

public class ApplicationUserOTPEntityTypeConfiguration : IEntityTypeConfiguration<ApplicationUserOTP>
{
    public void Configure(EntityTypeBuilder<ApplicationUserOTP> builder)
    {
        builder.Property(e => e.OTP).IsRequired().HasMaxLength(10);

        builder.HasOne(e => e.ApplicationUser)
            .WithMany()
            .HasForeignKey(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
