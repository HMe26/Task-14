using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CinemaApp.Models;

public class Cinema
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;


    [ValidateNever]
    public string Img { get; set; } = string.Empty;

    public bool Status { get; set; }
}
