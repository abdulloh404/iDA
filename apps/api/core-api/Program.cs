using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", (CancellationToken _) =>
        TypedResults.Ok(new HealthResponse("Healthy", DateTimeOffset.UtcNow)))
    .WithName("GetHealth")
    .WithSummary("Get the Core API health status")
    .WithDescription("Returns the current health status and server timestamp.")
    .Produces<HealthResponse>(StatusCodes.Status200OK);

app.Run();

/// <summary>แสดงสถานะพร้อมเวลาที่ตรวจสอบ Core API</summary>
public sealed record HealthResponse(string Status, DateTimeOffset CheckedAt);
