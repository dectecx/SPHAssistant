using SPHAssistant.Core.Interfaces;

namespace SPHAssistant.Worker;

/// <summary>
/// Represents the main background service for executing periodic tasks.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOcrService _ocrService;
    private const string CaptchaUrl = "https://rms.sph.org.tw/ValidateCode.aspx";
    private const string CaptchaBackupFolderName = "captchas";

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The factory for creating HTTP clients.</param>
    /// <param name="ocrService">The OCR recognition service.</param>
    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IOcrService ocrService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _ocrService = ocrService;
    }

    /// <summary>
    /// This method is called when the <see cref="IHostedService"/> starts. The implementation should start the task.
    /// </summary>
    /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
    /// <returns>A <see cref="Task"/>that represents the long running operations.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure the backup directory exists
        var captchaBackupPath = Path.Combine(AppContext.BaseDirectory, CaptchaBackupFolderName);
        Directory.CreateDirectory(captchaBackupPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                using var response = await httpClient.GetAsync(CaptchaUrl, stoppingToken);
                response.EnsureSuccessStatusCode();

                var imageBytes = await response.Content.ReadAsByteArrayAsync(stoppingToken);

                // Save the image for manual verification before processing
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var fileName = $"captcha_{timestamp}.gif";
                var filePath = Path.Combine(captchaBackupPath, fileName);
                await File.WriteAllBytesAsync(filePath, imageBytes, stoppingToken);
                _logger.LogInformation("Captcha image saved to: {FilePath}", filePath);

                // Use a new stream for OCR service
                using var imageStream = new MemoryStream(imageBytes);
                var recognizedText = await _ocrService.RecognizeCaptchaAsync(imageStream);

                if (!string.IsNullOrEmpty(recognizedText))
                {
                    _logger.LogInformation("Captcha recognized successfully: {Text}", recognizedText);
                }
                else
                {
                    _logger.LogWarning("Failed to recognize captcha.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the captcha.");
            }

            // Delay to avoid overwhelming the server
            await Task.Delay(1000, stoppingToken);
        }
    }
}
