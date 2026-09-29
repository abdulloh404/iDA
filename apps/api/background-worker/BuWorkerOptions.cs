using System.Text.RegularExpressions;
using Npgsql;

namespace iDA.Bu.Worker;

internal sealed class BuWorkerOptions
{
    private static readonly Regex BuIdPattern = new("^BU[0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private BuWorkerOptions(string id, string hospitalId, string queueName, string databaseConnectionString,
        string queueConnectionString, TimeSpan healthInterval, TimeSpan retryInterval, ushort prefetchCount)
    {
        Id = id;
        HospitalId = hospitalId;
        QueueName = queueName;
        DatabaseConnectionString = databaseConnectionString;
        QueueConnectionString = queueConnectionString;
        HealthInterval = healthInterval;
        RetryInterval = retryInterval;
        PrefetchCount = prefetchCount;
    }

    public string Id { get; }
    public string HospitalId { get; }
    public string QueueName { get; }
    public string DatabaseConnectionString { get; }
    public string QueueConnectionString { get; }
    public TimeSpan HealthInterval { get; }
    public TimeSpan RetryInterval { get; }
    public ushort PrefetchCount { get; }

    public static BuWorkerOptions Read(IConfiguration configuration, string[] args)
    {
        var id = ReadBuId(configuration, args);
        var hospitalId = FirstValue(configuration["Bu:HospitalId"], configuration[$"{id}_HOSPITAL_ID"], id)!;
        if (hospitalId.Length > 20) throw new InvalidOperationException("Bu:HospitalId must contain at most 20 characters.");

        var expectedQueueName = $"jobs.{id.ToLowerInvariant()}";
        var queueName = FirstValue(configuration["Bu:QueueName"], expectedQueueName)!;
        if (!string.Equals(queueName, expectedQueueName, StringComparison.Ordinal))
            throw new InvalidOperationException("Bu:QueueName must match the selected Bu:Id.");

        var databaseConnection = ReadDatabaseConnection(configuration, id);
        var queueConnection = FirstValue(configuration["Queue:ConnectionString"], configuration[$"{id}_QUEUE_CONNECTION"]);
        ValidateQueueConnection(queueConnection);

        var healthInterval = ReadInterval(configuration, "Worker:HealthIntervalSeconds", 30);
        var retryInterval = ReadInterval(configuration, "Worker:RetryIntervalSeconds", 5);
        var prefetchCount = ReadPrefetchCount(configuration);

        return new BuWorkerOptions(id, hospitalId, queueName, databaseConnection, queueConnection!, healthInterval, retryInterval, prefetchCount);
    }

    private static string ReadBuId(IConfiguration configuration, string[] args)
    {
        var value = ReadCommandLine(args, "Bu:Id", "BU_ID")
            ?? Environment.GetEnvironmentVariable("Bu__Id")
            ?? Environment.GetEnvironmentVariable("BU_ID")
            ?? configuration["Bu:Id"];
        value = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(value) || !BuIdPattern.IsMatch(value))
            throw new InvalidOperationException("Set Bu:Id or BU_ID to a BU number such as BU01.");
        return value;
    }

    private static string ReadDatabaseConnection(IConfiguration configuration, string id)
    {
        var configured = configuration.GetConnectionString("Bu");
        var password = FirstSecret(configuration["Database:Password"], configuration[$"{id}_DB_PASSWORD"], configuration["BU_DB_PASSWORD"]);
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = configuration.GetConnectionString(id);
            password = FirstSecret(configuration[$"{id}_DB_PASSWORD"], configuration["Database:Password"], configuration["BU_DB_PASSWORD"]);
        }
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = configuration[$"{id}_DB_CONNECTION"];
            password = FirstSecret(configuration[$"{id}_DB_PASSWORD"], configuration["BU_DB_PASSWORD"], configuration["Database:Password"]);
        }
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException($"Set ConnectionStrings:Bu, ConnectionStrings:{id}, or {id}_DB_CONNECTION.");

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(configured);
        }
        catch
        {
            throw new InvalidOperationException("The selected BU database connection string is invalid.");
        }

        if (string.IsNullOrWhiteSpace(builder.Password)) builder.Password = password;
        if (string.IsNullOrWhiteSpace(builder.Host) || string.IsNullOrWhiteSpace(builder.Database) ||
            string.IsNullOrWhiteSpace(builder.Username) || string.IsNullOrWhiteSpace(builder.Password))
            throw new InvalidOperationException("The selected BU database requires a host, database, runtime user, and password.");
        builder.IncludeErrorDetail = false;
        return builder.ConnectionString;
    }

    private static void ValidateQueueConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString) || !Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "amqp" && uri.Scheme != "amqps") || string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("Set Queue:ConnectionString or the selected BU queue connection to a valid AMQP URI.");
    }

    private static TimeSpan ReadInterval(IConfiguration configuration, string key, int defaultSeconds)
    {
        var value = configuration.GetValue(key, defaultSeconds);
        if (value is < 1 or > 300) throw new InvalidOperationException($"{key} must be between 1 and 300.");
        return TimeSpan.FromSeconds(value);
    }

    private static ushort ReadPrefetchCount(IConfiguration configuration)
    {
        var value = configuration.GetValue("Queue:PrefetchCount", 10);
        if (value is < 1 or > ushort.MaxValue)
            throw new InvalidOperationException("Queue:PrefetchCount must be between 1 and 65535.");
        return (ushort)value;
    }

    private static string? ReadCommandLine(string[] args, params string[] keys)
    {
        string? value = null;
        for (var index = 0; index < args.Length; index++)
        {
            foreach (var key in keys)
            {
                var prefix = $"--{key}=";
                if (args[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    value = args[index][prefix.Length..];
                else if (string.Equals(args[index], $"--{key}", StringComparison.OrdinalIgnoreCase) &&
                    index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    value = args[index + 1];
            }
        }
        return value;
    }

    private static string? FirstValue(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? FirstSecret(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
