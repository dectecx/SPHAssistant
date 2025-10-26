using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.DTOs;
using SPHAssistant.Core.Models.Enums;
using SPHAssistant.Core.Models.Result;
using SPHAssistant.Core.Models.TimeTable;

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

                // --- Test fetching and parsing the timetable ---
                await RunTimeTableTestAsync(scopedLogger, hospitalClient);


                // --- Keep the existing query logic for now ---
                // await RunQueryAndProcessResultAsync(scopedLogger, hospitalClient, tableGenerator);
            }

            _logger.LogInformation("Work item finished. Waiting for the next one.");
            // For demonstration, we run once and then stop. In a real service, you'd likely have a delay.
            // await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            break;
        }
    }

    private async Task RunTimeTableTestAsync(ILogger<Worker> logger, IHospitalClient hospitalClient)
    {
        // Test with "Orthopedics" department
        var departmentCode = "S2700A";
        logger.LogInformation("--- Starting Timetable Test for Department: {DepartmentCode} ---", departmentCode);

        var timeTable = await hospitalClient.GetTimeTableAsync(departmentCode);

        if (timeTable != null)
        {
            logger.LogInformation("Successfully fetched timetable for {DepartmentName} ({DepartmentCode})", timeTable.DepartmentName, timeTable.DepartmentCode);
            foreach (var daily in timeTable.DailyTimeTables)
            {
                var morningSlots = daily.MorningSlots;
                var afternoonSlots = daily.AfternoonSlots;
                var nightSlots = daily.NightSlots;

                if (morningSlots.Count > 0 || afternoonSlots.Count > 0 || nightSlots.Count > 0)
                {
                    logger.LogInformation("-> Date: {Date}", daily.Date);
                    LogSlotsForSession(logger, "   Morning", morningSlots);
                    LogSlotsForSession(logger, "   Afternoon", afternoonSlots);
                    LogSlotsForSession(logger, "   Night", nightSlots);
                }
            }
        }
        else
        {
            logger.LogError("Failed to fetch timetable for department {DepartmentCode}", departmentCode);
        }
        logger.LogInformation("--- Finished Timetable Test ---");
    }

    private void LogSlotsForSession(ILogger<Worker> logger, string sessionName, List<AppointmentSlot> slots)
    {
        if (slots.Count == 0)
        {
            return;
        }
        
        logger.LogInformation("{SessionName}:", sessionName);
        foreach (var slot in slots)
        {
            logger.LogInformation("     - Doctor: {DoctorName} (ID: {DoctorId}), Status: {Status}", 
                slot.Doctor.Name, 
                slot.Doctor.Id, 
                slot.Status);
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

            if (string.IsNullOrEmpty(success.ResponseHtml))
            {
                logger.LogError("Query successful, but the response HTML is empty.");
                return;
            }

            // Parse the HTML from the successful response to extract structured data.
            var appointmentData = hospitalClient.ParseAppointmentData(success.ResponseHtml);

            if (appointmentData.Rows.Count > 0)
            {
                // Generate a Markdown table from the structured data.
                string markdownTable = tableGenerator.Generate(appointmentData);

                // Log the final Markdown table.
                logger.LogInformation("Appointment Details:\n{MarkdownTable}", markdownTable);
            }
            else
            {
                logger.LogInformation("Query was successful, but the result table was empty.");
            }
        }
        else
        {
            logger.LogError(logMessage);
        }
    }
}
