namespace CinemaApp.Repositories;

public class BulkRepository<T> : Repository<T>, IBulkRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;

    public BulkRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task CreateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        await _context.AddRangeAsync(entities, ct);
    }

    public bool DeleteRange(IEnumerable<T> entities)
    {
        try
        {
            _context.RemoveRange(entities);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
}
