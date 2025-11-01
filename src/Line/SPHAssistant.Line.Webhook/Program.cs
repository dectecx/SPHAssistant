using isRock.LineBot;
using SPHAssistant.Line.Core.Interfaces;
using SPHAssistant.Line.Webhook.CommandHandlers;
using SPHAssistant.Line.Webhook.Handlers;
using SPHAssistant.Line.Webhook.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Configure and register LineBot services
var channelAccessToken = builder.Configuration["LineBot:ChannelAccessToken"];
if (string.IsNullOrEmpty(channelAccessToken))
{
    throw new InvalidOperationException("LineBot ChannelAccessToken is not configured.");
}
builder.Services.AddSingleton(new Bot(channelAccessToken));

// 3. Add custom application services
builder.Services.AddScoped<ILineWebhookHandler, LineWebhookHandler>();
builder.Services.AddScoped<ICommandRouter, CommandRouter>();
builder.Services.AddScoped<ILineReplyService, LineReplyService>();

// Register all command handlers
builder.Services.AddScoped<ICommandHandler, EchoCommandHandler>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
