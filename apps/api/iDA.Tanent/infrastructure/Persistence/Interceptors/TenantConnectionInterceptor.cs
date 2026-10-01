using System.Data.Common;
using Ida.Application.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Ida.Infrastructure.Persistence.Interceptors;

public class TenantConnectionInterceptor(ITenantContext tenant) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData, CancellationToken ct = default)
    {
        await ApplyAsync(connection, ct);
        await base.ConnectionOpenedAsync(connection, eventData, ct);
    }

    public override void ConnectionOpened(DbConnection connection,
        ConnectionEndEventData eventData)
    {
        ApplyAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    private async Task ApplyAsync(DbConnection connection, CancellationToken ct)
    {
        if (connection is not NpgsqlConnection npgsql) return;

        var hospitalId = tenant.HasTenant ? tenant.HospitalId : string.Empty;

        await using var command = npgsql.CreateCommand();
        command.CommandText = "SELECT set_config('app.hospital_id', $1, false)";
        command.Parameters.Add(new NpgsqlParameter { Value = hospitalId });
        await command.ExecuteNonQueryAsync(ct);
    }
}
