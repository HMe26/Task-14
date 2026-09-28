namespace CinemaApp.Models;

public class ApplicationUserOTP : Audit
{
    public int Id { get; set; }

    public string OTP { get; set; } = string.Empty;

    public bool IsUsed { get; set; } = false;
    public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMinutes(10);

    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;
}
