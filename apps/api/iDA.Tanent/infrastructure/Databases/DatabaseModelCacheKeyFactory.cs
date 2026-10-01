using Ida.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is IdaDbContext ida
            ? (context.GetType(), ida.Layout.CacheKey, designTime)
            : (context.GetType(), designTime);

    public object Create(DbContext context) => Create(context, false);
}
