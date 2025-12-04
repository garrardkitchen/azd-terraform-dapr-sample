using Dapr;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.Services.AddDaprClient();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCloudEvents();
app.MapSubscribeHandler();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries =
    ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapGet("dapr-test/data", (ILogger<Program> logger) =>
    {
        logger.LogInformation("dapr-test/data called!");
        return Results.Ok("Hello");
    })
    .WithName("dapr-data");

// app.MapPost("/subscriptions/weather", [Topic("pubsub", "weather")] (ILogger<Program> logger, WeatherForecastMessage message) =>
// {
//     logger.LogInformation("Weather forecast message received: {Message}", message.Message);
// });

app.MapPost("/subscriptions/sub1", [Topic("pubsub-servicebus", "topic")] (ILogger<Program> logger, WeatherForecastMessage message) =>
{
    logger.LogInformation("Weather forecast message received by pubsub-servicebus: {Message}", message.Message);
    return Results.Ok();
});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal sealed record WeatherForecastMessage(string Message);