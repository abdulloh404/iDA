using Npgsql;

namespace iDA.Bu.Worker;

internal sealed class BuDatabaseConnection(BuWorkerOptions options)
{
    public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(options.DatabaseConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT set_config('app.hospital_id', $1, false)";
            command.Parameters.AddWithValue(options.HospitalId);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
