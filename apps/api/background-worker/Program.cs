using iDA.Bu.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var options = BuWorkerOptions.Read(builder.Configuration, args, builder.Environment.IsDevelopment());
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<BuDatabaseConnection>();
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
