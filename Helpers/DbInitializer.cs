using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CinemaApp.Helpers;

public class DbInitializer : IDbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DbInitializer(ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public void Initialize()
    {
        // migrate db
        if (_context.Database.GetPendingMigrations().Any())
            _context.Database.Migrate();

        // seed roles
        if (_roleManager.Roles.IsNullOrEmpty())
        {
            _roleManager.CreateAsync(new(RoleConstants.SUPER_ADMIN)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new(RoleConstants.ADMIN)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new(RoleConstants.EMPLOYEE)).GetAwaiter().GetResult();

            // seed super account user
            ApplicationUser user = new()
            {
                UserName = "SuperAdmin",
                FirstName = "Super",
                LastName = "Admin",
                Email = "SuperAdmin@cinemaapp.com",
                EmailConfirmed = true
            };

            _userManager.CreateAsync(user, "Admin123@").GetAwaiter().GetResult();

            _userManager.AddToRoleAsync(user, RoleConstants.SUPER_ADMIN).GetAwaiter().GetResult();
        }

        // seed seats for movies created before the booking system existed
        var movieIdsWithSeats = _context.Set<Seat>().Select(s => s.MovieId).Distinct().ToHashSet();
        var movies = _context.Set<Movie>().Where(m => !movieIdsWithSeats.Contains(m.Id)).ToList();

        foreach (var movie in movies)
        {
            _context.Set<Seat>().AddRange(SeatMapGenerator.Generate(movie.Id, movie.TotalSeats));

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // seats already exist for this movie, skip it
                foreach (var entry in _context.ChangeTracker.Entries<Seat>().ToList())
                    entry.State = EntityState.Detached;
            }
        }
    }
}
