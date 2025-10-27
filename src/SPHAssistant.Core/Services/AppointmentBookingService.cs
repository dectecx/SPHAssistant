using Microsoft.Extensions.Logging;
using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.Booking;
using SPHAssistant.Core.Models.Booking.Result;
using HtmlAgilityPack;
using SPHAssistant.Core.Models.Internal;

namespace SPHAssistant.Core.Services;

/// <summary>
/// Implements the service for handling the appointment booking process.
/// </summary>
public class AppointmentBookingService : IAppointmentBookingService
{
    private readonly ILogger<AppointmentBookingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IOcrService _ocrService;
    private static readonly Random _random = new();

    /// <summary>
    /// The URL of the captcha page.
    /// </summary>
    private const string CaptchaUrl = "ValidateCode.aspx";

    /// <summary>
    /// Constructor
    /// </summary>
    public AppointmentBookingService(
        ILogger<AppointmentBookingService> logger,
        HttpClient httpClient,
        IOcrService ocrService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _ocrService = ocrService;
    }

    /// <inheritdoc/>
    public async Task<BookingStatus> BookAppointmentAsync(BookingRequest request)
    {
        const int maxCaptchaRetries = 5;
        BookingStatus? finalResult = null;

        try
        {
            // Step 1: Fetch the initial state of the booking page (Login.aspx)
            var loginPageUrl = $"Login.aspx?rmsData={request.Parameters.RmsData}&dptName={request.Parameters.DptName}&dpt={request.Parameters.Dpt}&dptDptuid={request.Parameters.DptDptuid}";
            var webFormsState = await FetchWebFormsStateAsync(loginPageUrl);
            if (webFormsState is null)
            {
                return new BookingOperationError("Failed to parse initial booking page state.");
            }

            for (int attempt = 1; attempt <= maxCaptchaRetries; attempt++)
            {
                _logger.LogInformation("Captcha attempt {Attempt}/{MaxAttempts}", attempt, maxCaptchaRetries);

                // Step 2: Recognize the captcha. 4 characters are expected.
                var captchaText = await RecognizeCaptchaAsync();
                if (string.IsNullOrEmpty(captchaText) || captchaText.Length != 4)
                {
                    _logger.LogWarning("Captcha attempt {Attempt} failed: The captcha is not recognized or the length is not 4. Captcha text: {CaptchaText}. Retrying in 1 second...", attempt, captchaText);
                    await Task.Delay(1000);
                    continue;
                }

                // Step 3: Post the booking form
                var resultHtml = await PostBookingFormAsync(request, webFormsState, captchaText, loginPageUrl);
                if (string.IsNullOrEmpty(resultHtml))
                {
                    return new BookingOperationError("Failed to get a response after posting the booking form.");
                }

                // Step 4: Analyze the response HTML
                finalResult = AnalyzeBookingResponseHtml(resultHtml);

                // If the result is a captcha error, retry the captcha.
                if (finalResult is BookingCaptchaError)
                {
                    if (attempt >= maxCaptchaRetries)
                    {
                        _logger.LogError("Failed to query appointment after {MaxAttempts} captcha attempts.", maxCaptchaRetries);
                        return new BookingOperationError($"Failed to book appointment after {maxCaptchaRetries} captcha attempts. The captcha is still not recognized.");
                    }

                    // Wait 1 second before the next attempt
                    _logger.LogWarning("Captcha attempt {Attempt} failed: {Message}. Retrying in 1 second...", attempt, finalResult.Message);
                    await Task.Delay(1000);
                    continue;
                }

                // If the result is successful or a definitive failure that is NOT a captcha error, exit the retry loop.
                _logger.LogInformation("Booking successful or a definitive failure that is NOT a captcha error. Exiting retry loop. Final status: {StatusType}", finalResult.GetType().Name);
                break;
            }

            // If the final result is still null, set it to an operation error.
            finalResult ??= new BookingOperationError($"Failed to book appointment after {maxCaptchaRetries} attempts.");
            return finalResult;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "An HTTP error occurred during the booking process.");
            return new BookingOperationError($"HTTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in AppointmentBookingService.");
            return new BookingOperationError($"Unexpected Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches the web forms state from the initial booking page.
    /// </summary>
    /// <param name="loginPageUrl">The URL of the initial booking page.</param>
    /// <returns>The web forms state.</returns>
    private async Task<WebFormsState?> FetchWebFormsStateAsync(string loginPageUrl)
    {
        _logger.LogInformation("Fetching initial booking page to get ViewState and session cookie.");

        var response = await _httpClient.GetAsync(loginPageUrl);
        response.EnsureSuccessStatusCode();
        var pageHtml = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(pageHtml);

        var viewState = doc.GetElementbyId("__VIEWSTATE")?.GetAttributeValue("value", "");
        var viewStateGenerator = doc.GetElementbyId("__VIEWSTATEGENERATOR")?.GetAttributeValue("value", "");
        var eventValidation = doc.GetElementbyId("__EVENTVALIDATION")?.GetAttributeValue("value", "");

        if (string.IsNullOrEmpty(viewState) || string.IsNullOrEmpty(eventValidation))
        {
            _logger.LogError("Could not extract __VIEWSTATE or __EVENTVALIDATION from the page.");
            return null;
        }

        _logger.LogInformation("Successfully extracted ViewState and other hidden fields.");
        return new WebFormsState(viewState, viewStateGenerator!, eventValidation);
    }

    /// <summary>
    /// Recognizes the captcha image using the OCR service.
    /// </summary>
    /// <returns>The recognized captcha text.</returns>
    private async Task<string> RecognizeCaptchaAsync()
    {
        _logger.LogInformation("Downloading captcha image.");
        var captchaStream = await _httpClient.GetStreamAsync(CaptchaUrl);

        _logger.LogInformation("Recognizing captcha with OCR service.");
        var captchaText = await _ocrService.RecognizeCaptchaAsync(captchaStream);

        if (string.IsNullOrEmpty(captchaText))
        {
            _logger.LogWarning("OCR service failed to recognize captcha.");
        }
        else
        {
            _logger.LogInformation("Captcha recognized as: {CaptchaText}", captchaText);
        }
        return captchaText;
    }

    /// <summary>
    /// Posts the booking form to the hospital website.
    /// </summary>
    /// <param name="request">The booking request.</param>
    /// <param name="state">The web forms state.</param>
    /// <param name="captchaText">The recognized captcha text.</param>
    /// <param name="loginPageUrl">The URL of the initial booking page.</param>
    /// <returns>The response HTML.</returns>
    private async Task<string> PostBookingFormAsync(
        BookingRequest request,
        WebFormsState state,
        string captchaText,
        string loginPageUrl)
    {
        _logger.LogInformation("Building and posting the booking form for ID {IdNumber}.", request.IdNumber);
        var visitType = request.IsFirstVisit ? "rdoFirst" : "rdoSeveral";

        var formData = new Dictionary<string, string>
        {
            { "__VIEWSTATE", state.ViewState },
            { "__VIEWSTATEGENERATOR", state.ViewStateGenerator },
            { "__EVENTVALIDATION", state.EventValidation },
            { "ctl00$ContentPlaceHolder1$rbnList", ((int)request.IdType).ToString() },
            { "ctl00$ContentPlaceHolder1$txtInput", request.IdNumber },
            { "ctl00$ContentPlaceHolder1$txtValidate", captchaText },
            // Simulate a random click on the image button to appear more human-like.
            { "ctl00$ContentPlaceHolder1$btnQuery.x", _random.Next(15, 91).ToString() },
            { "ctl00$ContentPlaceHolder1$btnQuery.y", _random.Next(15, 21).ToString() }
        };

        var postRequest = new HttpRequestMessage(HttpMethod.Post, loginPageUrl)
        {
            Content = new FormUrlEncodedContent(formData)
        };
        var postResponse = await _httpClient.SendAsync(postRequest);
        postResponse.EnsureSuccessStatusCode();
        return await postResponse.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Analyzes the booking response HTML.
    /// </summary>
    /// <param name="html">The response HTML.</param>
    /// <returns>The booking status.</returns>
    private BookingStatus AnalyzeBookingResponseHtml(string html)
    {
        _logger.LogInformation("Analyzing booking response HTML.");
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Priority 1: Check for success message
        var successNode = doc.GetElementbyId("ctl00_ContentPlaceHolder1_labMessage");
        if (successNode != null && !string.IsNullOrEmpty(successNode.InnerText))
        {
            var successMessage = successNode.InnerText.Trim();
            if (successMessage.Contains("掛號成功"))
            {
                _logger.LogInformation("Booking successful: {Message}", successMessage);
                return new BookingSuccess(successMessage, html);
            }
            // Handle cases where the slot might have just been taken
            if (successMessage.Contains("已額滿") || successMessage.Contains("預約名額已滿"))
            {
                _logger.LogWarning("Booking failed: Slot is already full. Message: {Message}", successMessage);
                return new SlotUnavailableError(successMessage, html);
            }
        }

        // Priority 2: Check for specific validation errors
        // Using a helper function to reduce redundant code
        Func<string, BookingStatus?> checkError = (id) => {
            var node = doc.GetElementbyId(id);
            if (node != null && !string.IsNullOrEmpty(node.InnerText.Trim()))
            {
                var errorMessage = node.InnerText.Trim();
                _logger.LogWarning("Booking validation error found. ID: {Id}, Message: {Message}", id, errorMessage);

                if (id.Contains("validateImg")) // Captcha errors
                    return new BookingCaptchaError(errorMessage, html);
                
                return new BookingValidationError(errorMessage, html); // Other validation errors
            }
            return null;
        };

        var errorStatus = checkError("ctl00_ContentPlaceHolder1_validateImg")
            ?? checkError("ctl00_ContentPlaceHolder1_validateInput");
        
        if (errorStatus != null)
        {
            return errorStatus;
        }
        
        _logger.LogError("Could not determine booking result from response HTML. It's not a known success or failure pattern.");
        return new UnknownBookingResponse("未知的掛號回應格式", html);
    }
}
