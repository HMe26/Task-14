using System.ComponentModel.DataAnnotations;

namespace CinemaApp.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool Status { get; set; }
}
