namespace CinemaApp.ViewModels;

public record MovieFilterVM(string? name, decimal? minPrice, decimal? maxPrice, int? categoryId, int? cinemaId);
