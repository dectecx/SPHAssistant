using Serilog;
using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Services;
using SPHAssistant.Worker;
using SPHAssistant.Core.Infrastructure.TableGenerators;
using System.Net;

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext())
        .ConfigureServices((hostContext, services) =>
        {
            // Register IHttpClientFactory and configure a typed client for IHospitalClient.
            // This handles the lifecycle of HttpClient and its handlers automatically.
            services.AddHttpClient<IHospitalClient, HospitalClient>(client =>
            {
                client.BaseAddress = new Uri("https://rms.sph.org.tw/");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = true
            });

            services.AddSingleton<IOcrService, OcrService>();
            services.AddSingleton<ITableGenerator, MarkdownTableGenerator>();
            
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
