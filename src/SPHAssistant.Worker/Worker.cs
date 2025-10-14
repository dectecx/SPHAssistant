using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models;
using SPHAssistant.Core.Models.DTOs;
using SPHAssistant.Core.Models.Enums;

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
    /// <param name="logger">The logger instance.</param>
    /// <param name="hospitalClient">The client for interacting with the hospital website.</param>
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

        if (result.IsSuccess)
        {
            _logger.LogInformation("Query successful. Message: {Message}", result.Message);
            // In a real scenario, you would parse result.ResponseHtml here.
        }
        else
        {
            _logger.LogError("Query failed. Message: {Message}", result.Message);
        }
    }
}
