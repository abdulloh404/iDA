using Ida.Infrastructure.Configuration;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Persistence;

public class IdaDbContextFactory : IDesignTimeDbContextFactory<IdaDbContext>
{
    public IdaDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddIdaSettings().AddCommandLine(args).Build();
        using var registry = new DatabaseRegistry(config);
        return DatabaseContexts.CreateSchemaContext(registry.FixedBranch, registry.CoreSchemaName, registry.ConnectionString(registry.FixedBranch, administrator: true));
    }
}
