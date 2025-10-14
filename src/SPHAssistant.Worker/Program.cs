using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Services;
using SPHAssistant.Worker;
using Serilog;

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext())
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHttpClient();
            services.AddSingleton<IOcrService, OcrService>();
            services.AddHostedService<Worker>();
        })
        .Build();

    Log.Information("Starting SPHAssistant Worker host");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SPHAssistant Worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
