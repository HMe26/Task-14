namespace CinemaApp.Repositories.IRepositories;

public interface IBulkRepository<T> : IRepository<T> where T : class
{
    Task CreateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    bool DeleteRange(IEnumerable<T> entities);
}
