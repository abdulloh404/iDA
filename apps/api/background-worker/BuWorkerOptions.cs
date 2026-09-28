namespace iDA.Bu.Worker;

internal sealed class BuWorkerOptions
{
    public const string SectionName = "Bu";

    public string Id { get; set; } = string.Empty;

    public string QueueName { get; set; } = string.Empty;
}
