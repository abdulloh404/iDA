using iDA.Bu.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptions<BuWorkerOptions>()
    .Bind(builder.Configuration.GetSection(BuWorkerOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Id), "Bu:Id is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.QueueName), "Bu:QueueName is required.")
    .Validate(options => options.QueueName == $"jobs.{options.Id.ToLowerInvariant()}", "Bu:QueueName must match Bu:Id.")
    .ValidateOnStart();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
