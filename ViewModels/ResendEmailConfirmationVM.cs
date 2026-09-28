using System.ComponentModel.DataAnnotations;

namespace CinemaApp.ViewModels;

public class ResendEmailConfirmationVM
{
    [Required(ErrorMessage = "Email or UserName is required.")]
    [Display(Name = "Email or UserName")]
    public string EmailOrUserName { get; set; } = string.Empty;
}
