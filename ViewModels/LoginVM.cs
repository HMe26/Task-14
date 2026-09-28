using System.ComponentModel.DataAnnotations;

namespace CinemaApp.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Email or UserName is required.")]
    [Display(Name = "Email or UserName")]
    public string EmailOrUserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember Me")]
    public bool Remember { get; set; }
}
