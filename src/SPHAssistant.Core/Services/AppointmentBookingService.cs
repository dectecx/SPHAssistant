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
            // Step 1: Fetch the initial state of the booking page (Login.aspx).
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

                // Step 3: Post the form.
                var verificationHtml = await PostIdentityVerificationAsync(request, webFormsState, captchaText, loginPageUrl);
                if (string.IsNullOrEmpty(verificationHtml))
                {
                    return new BookingOperationError("Failed to get a response after posting identity verification.");
                }

                // Step 4: Analyze the response.
                var verificationResult = AnalyzeLoginResponse(verificationHtml);

                // Handle the different outcomes of the verification step.
                switch (verificationResult)
                {
                    // Case A: Verification failed with a definitive status (e.g., slot full, validation error).
                    case VerificationFailed failed when failed.Status is not BookingCaptchaError:
                        _logger.LogWarning("Identity verification failed with a definitive error: {Message}", failed.Status.Message);
                        return failed.Status;

                    // Case B: Verification failed due to a captcha error. Retry the loop.
                    case VerificationFailed failed when failed.Status is BookingCaptchaError:
                        _logger.LogWarning("Captcha attempt {Attempt} failed: {Message}. Retrying...", attempt, failed.Status.Message);
                        if (attempt >= maxCaptchaRetries)
                        {
                            return new BookingOperationError($"Failed after {maxCaptchaRetries} captcha attempts.");
                        }
                        await Task.Delay(1000);
                        continue; // Continue to the next iteration of the for loop.

                    // Case C: This is a new patient. Flow is not implemented.
                    case NewPatientRegistrationRequired:
                        _logger.LogInformation("New patient registration is required. This flow is not yet implemented.");
                        // TODO: Implement the new patient registration flow.
                        return new BookingOperationError("New patient registration is not yet supported.");

                    // Case D: Success! We are on the confirmation page for a returning patient.
                    case ConfirmationRequired confirmation:
                        _logger.LogInformation("Identity verification successful. Proceeding to final confirmation.");
                        // Step 4: Post the final confirmation.
                        var finalHtml = await PostConfirmationAsync(request, confirmation.State, loginPageUrl);
                        // Step 5: Analyze the final response to get the outcome.
                        finalResult = AnalyzeConfirmationResponse(finalHtml);
                        break;
                }
            }

            return finalResult ?? new BookingOperationError($"Booking failed after {maxCaptchaRetries} attempts.");
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
    /// Analyzes the response of the initial identity verification step.
    /// </summary>
    /// <param name="html">The response HTML from the identity verification POST request.</param>
    /// <returns>An <see cref="IdentityVerificationResult"/> indicating the next step.</returns>
    private IdentityVerificationResult AnalyzeLoginResponse(string html)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Posts the second step of the booking process (confirmation) for a returning patient.
    /// </summary>
    /// <param name="request">The original booking request containing patient details.</param>
    /// <param name="confirmationState">The WebForms state from the confirmation page.</param>
    /// <param name="loginPageUrl">The URL of the previous page (Login.aspx) to be used as the referrer.</param>
    /// <returns>The final response HTML after submitting the confirmation.</returns>
    private Task<string> PostConfirmationAsync(BookingRequest request, WebFormsState confirmationState, string loginPageUrl)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Analyzes the final response HTML after posting the confirmation to determine the booking outcome.
    /// </summary>
    /// <param name="html">The final response HTML from the confirmation POST request.</param>
    /// <returns>A <see cref="BookingStatus"/> indicating the final outcome.</returns>
    private BookingStatus AnalyzeConfirmationResponse(string html)
    {
        throw new NotImplementedException();
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
    /// Posts the first step of the booking process (identity verification) to the Login.aspx page.
    /// </summary>
    /// <param name="request">The booking request containing identity information.</param>
    /// <param name="state">The initial WebForms state from the Login.aspx page.</param>
    /// <param name="captchaText">The recognized captcha text.</param>
    /// <param name="loginPageUrl">The full URL of the Login.aspx page with query parameters.</param>
    /// <returns>The response HTML from the identity verification post.</returns>
    private async Task<string> PostIdentityVerificationAsync(
        BookingRequest request,
        WebFormsState state,
        string captchaText,
        string loginPageUrl)
    {
        _logger.LogInformation("Posting identity verification for ID {IdNumber}.", request.IdNumber);

        var formData = new Dictionary<string, string>
        {
            { "__VIEWSTATE", state.ViewState },
            { "__VIEWSTATEGENERATOR", state.ViewStateGenerator },
            { "__EVENTVALIDATION", state.EventValidation },
            { "ctl00$ContentPlaceHolder1$rbnList", ((int)request.IdType).ToString() },
            { "ctl00$ContentPlaceHolder1$txtInput", request.IdNumber },
            { "ctl00$ContentPlaceHolder1$txtValidate", captchaText },
            // Simulate a random click on the image button to appear more human-like.
            { "ctl00$ContentPlaceHolder1$btnSend.x", _random.Next(15, 91).ToString() },
            { "ctl00$ContentPlaceHolder1$btnSend.y", _random.Next(15, 21).ToString() }
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
        throw new NotImplementedException();
    }
}
