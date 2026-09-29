using Ida.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Ida.Infrastructure.Persistence;

internal sealed class CrudRelatedData(IServiceProvider services, IQueryExecutor executor) : ICrudRelatedData
{
    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => services.GetRequiredService<IRepository<TEntity>>().Query();

    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct) => executor.ToListAsync(query, ct);

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct) => executor.FirstOrDefaultAsync(query, ct);
}
