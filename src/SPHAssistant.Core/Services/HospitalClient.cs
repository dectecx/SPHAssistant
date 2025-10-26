using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.DTOs;
using SPHAssistant.Core.Models.Enums;
using SPHAssistant.Core.Models.Result;
using System.Net;

namespace SPHAssistant.Core.Services;

/// <summary>
/// Implements the client for interacting with the St. Paul's Hospital website.
/// </summary>
public class HospitalClient : IHospitalClient
{
    private readonly HttpClient _httpClient;
    private readonly IOcrService _ocrService;
    private readonly ILogger<HospitalClient> _logger;
    private static readonly Random _random = new();

    /// <summary>
    /// The base URL of the hospital website.
    /// </summary>
    private const string BaseUrl = "https://rms.sph.org.tw/";

    /// <summary>
    /// The URL of the query page.
    /// </summary>
    private const string QueryPageUrl = "Query.aspx?loc=S";

    /// <summary>
    /// The URL of the captcha page.
    /// </summary>
    private const string CaptchaUrl = "ValidateCode.aspx";

    /// <summary>
    /// Represents the web forms state.
    /// </summary>
    /// <param name="ViewState">The view state of the web forms.</param>
    /// <param name="ViewStateGenerator">The view state generator of the web forms.</param>
    /// <param name="EventValidation">The event validation of the web forms.</param>
    private record WebFormsState(string ViewState, string ViewStateGenerator, string EventValidation);

    /// <summary>
    /// Defines the type of error check to perform on the span node.
    /// </summary>
    private enum ErrorCheckType
    {
        /// <summary>
        /// Check the style of the span node. If the style contains "display:none" or "visibility:hidden", then it is not an error.
        /// </summary>
        Style,

        /// <summary>
        /// Check the inner text of the span node. If the inner text is not empty, then it is an error.
        /// </summary>
        InnerText
    }

    /// <summary>
    /// Represents the error definition.
    /// </summary>
    /// <param name="Id">The ID of the span node.</param>
    /// <param name="CheckType">The type of error check to perform on the span node.</param>
    /// <param name="CreateStatus">The function to create the status.</param>
    private record ErrorDefinition(string Id, ErrorCheckType CheckType, Func<string, string, QueryStatus> CreateStatus);

    /// <summary>
    /// The list of error definitions.
    /// </summary>
    private readonly List<ErrorDefinition> _errorDefinitions;

    /// <summary>
    /// Constructor
    /// </summary>
    public HospitalClient(ILogger<HospitalClient> logger, IOcrService ocrService)
    {
        _logger = logger;
        _ocrService = ocrService;

        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");

        // Initialize the structured error definitions.
        _errorDefinitions = new List<ErrorDefinition>
        {
            // 輸入的值與圖片中的不符
            new("ctl00_ContentPlaceHolder1_validateImg", ErrorCheckType.Style, (msg, html) => new CaptchaError(msg, html)),
            // 出生日期不可為空白!
            new("ctl00_ContentPlaceHolder1_validatBirthday1", ErrorCheckType.Style, (msg, html) => new ValidationError(msg, html)),
            // 出生日期輸入格式錯誤!
            new("ctl00_ContentPlaceHolder1_validatBirthday2", ErrorCheckType.Style, (msg, html) => new ValidationError(msg, html)),
            // 身份證字號輸入格式錯誤! or 身分證字號、病歷號或居留證號請擇一輸入，身分資料不可為空白!
            new("ctl00_ContentPlaceHolder1_validateInputS", ErrorCheckType.InnerText, (msg, html) => new ValidationError(msg, html)),
            // 您選擇的身分類型<複診>， 查詢院區<聖保祿醫院>，請確認是否正確!
            new("ctl00_ContentPlaceHolder1_txtInputSError", ErrorCheckType.InnerText, (msg, html) => new ValidationError(msg, html)),
            // 您輸入的出生日期有誤，請再次確認!
            new("ctl00_ContentPlaceHolder1_labBirthError", ErrorCheckType.InnerText, (msg, html) => new ValidationError(msg, html))
        };
    }

    /// <inheritdoc/>
    public async Task<QueryStatus> QueryAppointmentAsync(QueryRequest request)
    {
        const int maxCaptchaRetries = 5;
        QueryStatus? finalResult = null;

        try
        {
            // Step 1: Get session cookies and initial Web Forms state. This only needs to be done once.
            var webFormsState = await FetchWebFormsStateAsync();
            if (webFormsState is null)
            {
                return new OperationError("Failed to parse initial page state.");
            }

            for (int attempt = 1; attempt <= maxCaptchaRetries; attempt++)
            {
                _logger.LogInformation("Captcha attempt {Attempt}/{MaxAttempts}", attempt, maxCaptchaRetries);

                // Step 2: Recognize the captcha. 4 characters are expected.
                var captchaText = await RecognizeCaptchaInternalAsync();
                if (string.IsNullOrEmpty(captchaText) || captchaText.Length != 4)
                {
                    _logger.LogWarning("Captcha attempt {Attempt} failed: The captcha is not recognized or the length is not 4. Captcha text: {CaptchaText}. Retrying in 1 second...", attempt, captchaText);
                    await Task.Delay(1000);
                    continue;
                }

                // Step 3: Post the form.
                var resultHtml = await PostQueryFormAsync(request, webFormsState, captchaText);

                // Step 4: Analyze the response.
                finalResult = AnalyzeResponseHtml(resultHtml);

                // If the query is a captcha error, retry the captcha.
                if (finalResult is CaptchaError)
                {
                    if (attempt >= maxCaptchaRetries)
                    {
                        _logger.LogError("Failed to query appointment after {MaxAttempts} captcha attempts.", maxCaptchaRetries);
                        return new OperationError($"Failed to query appointment after {maxCaptchaRetries} captcha attempts. The captcha is still not recognized.");
                    }

                    // Wait 1 second before the next attempt
                    _logger.LogWarning("Captcha attempt {Attempt} failed: {Message}. Retrying in 1 second...", attempt, finalResult.Message);
                    await Task.Delay(1000);
                    continue;
                }

                // If the query is successful or a definitive failure that is NOT a captcha error, exit the retry loop.
                _logger.LogInformation("Query successful or a definitive failure that is NOT a captcha error. Exiting retry loop. Final status: {StatusType}", finalResult.GetType().Name);
                break;
            }

            // If the final result is still null, set it to an operation error.
            finalResult ??= new OperationError($"Failed to query appointment after {maxCaptchaRetries} attempts.");
            return finalResult;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "An HTTP error occurred while communicating with the hospital website.");
            return new OperationError($"HTTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in HospitalClient.");
            return new OperationError($"Unexpected Error: {ex.Message}");
        }
    }

    private async Task<WebFormsState?> FetchWebFormsStateAsync()
    {
        _logger.LogInformation("Fetching initial query page to get ViewState and cookies.");
        var initialResponse = await _httpClient.GetAsync(QueryPageUrl);
        initialResponse.EnsureSuccessStatusCode();
        var pageHtml = await initialResponse.Content.ReadAsStringAsync();

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

    private async Task<string> RecognizeCaptchaInternalAsync()
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

    private async Task<string> PostQueryFormAsync(QueryRequest request, WebFormsState state, string captchaText)
    {
        _logger.LogInformation("Building and posting the query form.");
        var timesValue = request.QueryType switch
        {
            QueryType.ReturningPatient => "rbnSeveralTimes",
            QueryType.NewPatient => "rbnFirstTime",
            _ => throw new ArgumentOutOfRangeException(nameof(request.QueryType))
        };

        var formData = new Dictionary<string, string>
        {
            { "__VIEWSTATE", state.ViewState },
            { "__VIEWSTATEGENERATOR", state.ViewStateGenerator },
            { "__EVENTVALIDATION", state.EventValidation },
            { "ctl00$ContentPlaceHolder1$Times", timesValue },
            { "ctl00$ContentPlaceHolder1$rbnListS", ((int)request.IdType).ToString() },
            { "ctl00$ContentPlaceHolder1$txtInputS", request.IdNumber },
            { "ctl00$ContentPlaceHolder1$txtBirthday", request.BirthDate },
            { "ctl00$ContentPlaceHolder1$txtValidate", captchaText },
            // Simulate a random click on the image button to appear more human-like.
            { "ctl00$ContentPlaceHolder1$btnQuery.x", _random.Next(15, 91).ToString() },
            { "ctl00$ContentPlaceHolder1$btnQuery.y", _random.Next(15, 21).ToString() }
        };

        var postRequest = new HttpRequestMessage(HttpMethod.Post, QueryPageUrl) { Content = new FormUrlEncodedContent(formData) };
        var postResponse = await _httpClient.SendAsync(postRequest);
        postResponse.EnsureSuccessStatusCode();
        return await postResponse.Content.ReadAsStringAsync();
    }

    private QueryStatus AnalyzeResponseHtml(string resultHtml)
    {
        _logger.LogInformation("Analyzing response HTML for success or specific error indicators.");
        var resultDoc = new HtmlDocument();
        resultDoc.LoadHtml(resultHtml);

        // Priority 1: Check for the success table.
        var successTable = resultDoc.GetElementbyId("ctl00_ContentPlaceHolder1_gvQueryResult");
        if (successTable != null)
        {
            _logger.LogInformation("Query successful: Found the result table 'gvQueryResult'.");
            return new QuerySuccess(resultHtml);
        }

        // Priority 2: Check against the structured error definitions.
        foreach (var definition in _errorDefinitions)
        {
            var spanNode = resultDoc.GetElementbyId(definition.Id);
            if (spanNode == null) continue;

            bool isError = false;
            var errorMessage = spanNode.InnerText.Trim();

            if (definition.CheckType == ErrorCheckType.Style)
            {
                var style = spanNode.GetAttributeValue("style", "");
                isError = !style.Contains("display:none") && !style.Contains("visibility:hidden");
            }
            else // InnerText
            {
                isError = !string.IsNullOrWhiteSpace(errorMessage);
            }

            if (isError)
            {
                _logger.LogWarning("Query failed: A known error pattern was detected. ID: {SpanId}, Message: {ErrorMessage}", definition.Id, errorMessage);
                return definition.CreateStatus(errorMessage, resultHtml);
            }
        }

        // Priority 3: Check for the "No Data Found" panel.
        var noDataPanel = resultDoc.GetElementbyId("ctl00_ContentPlaceHolder1_panelFailResult");
        if (noDataPanel != null)
        {
            var noDataMessage = noDataPanel.SelectSingleNode(".//strong")?.InnerText.Trim() ?? "目前查無您的掛號資料!";
            _logger.LogWarning("Query failed: The 'No Data Found' panel was displayed. Message: {Message}", noDataMessage);
            return new DataNotFound(noDataMessage, resultHtml);
        }

        _logger.LogError("Query failed: Could not determine the result from the response HTML. It's not a known success or failure pattern.");
        return new UnknownResponse("未知的回應格式", resultHtml);
    }
}
