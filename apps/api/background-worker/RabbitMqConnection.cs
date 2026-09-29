using RabbitMQ.Client;

namespace iDA.Bu.Worker;

internal sealed class RabbitMqConnection(BuWorkerOptions options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task VerifyAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
            try
            {
                await _channel!.QueueDeclarePassiveAsync(options.QueueName, cancellationToken);
            }
            catch
            {
                await DisposeChannelAsync();
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true }) return;

        await DisposeResourcesAsync();
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(options.QueueConnectionString),
                AutomaticRecoveryEnabled = false
            };
            _connection = await factory.CreateConnectionAsync($"ida-{options.Id.ToLowerInvariant()}-worker", cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.BasicQosAsync(0, options.PrefetchCount, false, cancellationToken);
        }
        catch
        {
            await DisposeResourcesAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await DisposeResourcesAsync();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task DisposeResourcesAsync()
    {
        await DisposeChannelAsync();
        var connection = _connection;
        _connection = null;
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private async Task DisposeChannelAsync()
    {
        var channel = _channel;
        _channel = null;
        if (channel is not null)
        {
            await channel.DisposeAsync();
        }
    }
}
