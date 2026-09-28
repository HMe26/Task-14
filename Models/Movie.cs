using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CinemaApp.Models;

public class Movie
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Des { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    public bool Status { get; set; }
    public DateTime DateTime { get; set; }

    [Range(1, 500, ErrorMessage = "Please enter a valid number of seats.")]
    public int TotalSeats { get; set; } = 100;

    [ValidateNever]
    public string MainImg { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    public int CategoryId { get; set; }

    [ValidateNever]
    public Category Category { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a cinema.")]
    public int CinemaId { get; set; }

    [ValidateNever]
    public Cinema Cinema { get; set; } = null!;

    [ValidateNever]
    public ICollection<MovieSubImg> SubImages { get; set; } = new List<MovieSubImg>();

    [ValidateNever]
    public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();

    [ValidateNever]
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
