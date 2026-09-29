using Npgsql;

namespace Ida.Infrastructure.Databases.Provisioning;

internal static class ProvisioningSql
{
    public static string Identifier(string value) => DatabaseRegistry.Quote(value);

    public static string Literal(string value) => "'" + value.Replace("'", "''") + "'";

    public static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task<T?> ScalarAsync<T>(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        var result = await command.ExecuteScalarAsync(ct);
        if (result is null or DBNull) return default;
        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string ResolveAsset(string directory, string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, directory, name);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Database provisioning asset '{directory}/{name}' was not copied to the application output.", path);
        return path;
    }
}
