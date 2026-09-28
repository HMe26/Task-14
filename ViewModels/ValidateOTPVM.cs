using System.ComponentModel.DataAnnotations;

namespace CinemaApp.ViewModels;

public class ValidateOTPVM
{
    [Required(ErrorMessage = "OTP is required.")]
    [Display(Name = "OTP Code")]
    public string OTP { get; set; } = string.Empty;
}
