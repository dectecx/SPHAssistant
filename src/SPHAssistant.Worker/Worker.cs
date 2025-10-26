using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.DTOs;
using SPHAssistant.Core.Models.Enums;
using SPHAssistant.Core.Models.Result;

namespace SPHAssistant.Worker;

/// <summary>
/// Represents the main background service for executing periodic tasks.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        // This loop simulates the worker waiting for tasks.
        // In a real scenario, this might be triggered by a message queue, a timer, or a database check.
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting a new work item. Creating a new scope.");

            // Create a new DI scope for each unit of work.
            // This ensures that scoped services (like HospitalClient) are unique to this work item.
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Worker>>();
                var hospitalClient = scope.ServiceProvider.GetRequiredService<IHospitalClient>();
                var tableGenerator = scope.ServiceProvider.GetRequiredService<ITableGenerator>();

                await RunQueryAndProcessResultAsync(scopedLogger, hospitalClient, tableGenerator);
            }

            _logger.LogInformation("Work item finished. Waiting for the next one.");
            // For demonstration, we run once and then stop. In a real service, you'd likely have a delay.
            // await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            break; 
        }
    }

    private async Task RunQueryAndProcessResultAsync(ILogger<Worker> logger, IHospitalClient hospitalClient, ITableGenerator tableGenerator)
    {
        var testRequest = new QueryRequest(
            QueryType: QueryType.ReturningPatient,
            IdType: IdType.IdCard,
            IdNumber: "A123456789",
            BirthDate: "0101" // MMdd format
        );

        logger.LogInformation("Attempting to query appointment with test data: {@QueryRequest}", testRequest);

        var result = await hospitalClient.QueryAppointmentAsync(testRequest);

        // Handle the result using a switch expression for type safety and clarity.
        var logMessage = result switch
        {
            QuerySuccess => $"Query successful. HTML length: {result.ResponseHtml?.Length ?? 0}",
            CaptchaError => $"Query failed: Captcha error. Message: {result.Message}",
            DataNotFound => $"Query failed: Data not found. Message: {result.Message}",
            ValidationError => $"Query failed: Validation error. Message: {result.Message}",
            OperationError => $"Query failed: Operation error. Message: {result.Message}",
            UnknownResponse => $"Query failed: Unknown response from server. Message: {result.Message}",
            _ => "Query failed with an unexpected result type."
        };

        if (result is QuerySuccess success)
        {
            logger.LogInformation(logMessage);

            // TODO: Call the new ParseAppointmentData method and generate the table.
        }
        else
        {
            logger.LogError(logMessage);
        }
    }
}
