using Microsoft.Extensions.Options;

namespace iDA.Bu.Worker;

internal sealed class Worker(ILogger<Worker> logger, IOptions<BuWorkerOptions> options) : BackgroundService
{
    private readonly BuWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker scaffold initialized for {BuId} with queue {QueueName}", _options.Id, _options.QueueName);
        logger.LogWarning("Queue consumption, Core API access and database writes are not implemented in this scaffold.");
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
