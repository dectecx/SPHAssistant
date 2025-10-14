using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Services;
using SPHAssistant.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddSingleton<IOcrService, OcrService>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
