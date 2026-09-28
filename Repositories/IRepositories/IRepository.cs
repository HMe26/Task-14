using System.Linq.Expressions;

namespace CinemaApp.Repositories.IRepositories;

public interface IRepository<T> where T : class
{
    Task<bool> CreateAsync(T entity, CancellationToken ct = default);

    bool Update(T entity);

    bool Delete(T entity);

    Task<int> CommitAsync(CancellationToken ct = default);

    IQueryable<T> Get(
        Expression<Func<T, bool>>? expression = null,
        Expression<Func<T, object>>[]? includes = null,
        bool tracked = true);

    T? GetOne(
        Expression<Func<T, bool>>? expression = null,
        Expression<Func<T, object>>[]? includes = null,
        bool tracked = true);
}
