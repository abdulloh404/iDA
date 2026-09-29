namespace iDA.Bu.Worker;

internal sealed class Worker(
    ILogger<Worker> logger,
    BuWorkerOptions options,
    BuDatabaseConnection database,
    RabbitMqConnection queue) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker connectivity setup started for {BuId} with queue {QueueName}", options.Id, options.QueueName);
        var ready = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            var databaseReady = await CheckDatabase(stoppingToken);
            var queueReady = await CheckQueue(stoppingToken);
            var currentReady = databaseReady && queueReady;
            if (currentReady && !ready)
                logger.LogInformation("Database and queue connectivity verified for {BuId}", options.Id);
            else if (!currentReady && ready)
                logger.LogWarning("Worker connectivity is unavailable for {BuId}; retrying", options.Id);
            ready = currentReady;

            var delay = ready ? options.HealthInterval : options.RetryInterval;
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task<bool> CheckDatabase(CancellationToken stoppingToken)
    {
        try
        {
            await using var connection = await database.OpenAsync(stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning("BU database check failed for {BuId} ({ErrorType})", options.Id, exception.GetType().Name);
            return false;
        }
    }

    private async Task<bool> CheckQueue(CancellationToken stoppingToken)
    {
        try
        {
            await queue.VerifyAsync(stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning("Queue check failed for {BuId} and {QueueName} ({ErrorType})", options.Id, options.QueueName, exception.GetType().Name);
            return false;
        }
    }
}
