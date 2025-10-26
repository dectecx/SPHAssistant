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
    private readonly IHospitalClient _hospitalClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    public Worker(ILogger<Worker> logger, IHospitalClient hospitalClient)
    {
        _logger = logger;
        _hospitalClient = hospitalClient;
    }

    /// <summary>
    /// This method is called when the <see cref="IHostedService"/> starts. The implementation should start the task.
    /// </summary>
    /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
    /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        var testRequest = new QueryRequest(
            QueryType: QueryType.ReturningPatient,
            IdType: IdType.IdCard,
            IdNumber: "A123456789",
            BirthDate: "0101" // MMdd format
        );
        
        _logger.LogInformation("Attempting to query appointment with test data: {@QueryRequest}", testRequest);

        var result = await _hospitalClient.QueryAppointmentAsync(testRequest);

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

        if (result is QuerySuccess)
        {
            _logger.LogInformation(logMessage);
        }
        else
        {
            _logger.LogError(logMessage);
        }
    }
}
