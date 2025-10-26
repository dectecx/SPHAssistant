using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.DTOs;
using SPHAssistant.Core.Models.Enums;
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
    private const string BaseUrl = "https://rms.sph.org.tw/";
    private const string QueryPageUrl = "Query.aspx?loc=S";
    private const string CaptchaUrl = "ValidateCode.aspx";

    private record WebFormsState(string ViewState, string ViewStateGenerator, string EventValidation);

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

        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36");
    }

    /// <inheritdoc/>
    public async Task<QueryResult> QueryAppointmentAsync(QueryRequest request)
    {
        try
        {
            // Step 1: Get session cookies and initial Web Forms state.
            var webFormsState = await FetchWebFormsStateAsync();
            if (webFormsState is null)
            {
                return new QueryResult(false, "Failed to parse initial page state.");
            }

            // Step 2: Recognize the captcha.
            var captchaText = await RecognizeCaptchaInternalAsync();
            if (string.IsNullOrEmpty(captchaText))
            {
                return new QueryResult(false, "Captcha recognition failed.");
            }

            // Step 3: Post the form.
            var resultHtml = await PostQueryFormAsync(request, webFormsState, captchaText);

            // Step 4: Analyze the response.
            return AnalyzeResponseHtml(resultHtml);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "An HTTP error occurred while communicating with the hospital website.");
            return new QueryResult(false, $"HTTP Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in HospitalClient.");
            return new QueryResult(false, $"Unexpected Error: {ex.Message}");
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

    private QueryResult AnalyzeResponseHtml(string resultHtml)
    {
        _logger.LogInformation("Analyzing response HTML for success or specific error indicators.");
        var resultDoc = new HtmlDocument();
        resultDoc.LoadHtml(resultHtml);

        var successTable = resultDoc.GetElementbyId("ctl00_ContentPlaceHolder1_gvQueryResult");
        if (successTable != null)
        {
            _logger.LogInformation("Query successful: Found the result table 'gvQueryResult'.");
            return new QueryResult(true, "查詢成功", resultHtml);
        }

        var styleBasedErrorSpans = new Dictionary<string, string>
        {
            { "ctl00_ContentPlaceHolder1_validatBirthday1", "出生日期不可為空白!" },
            { "ctl00_ContentPlaceHolder1_validatBirthday2", "出生日期輸入格式錯誤!" },
            { "ctl00_ContentPlaceHolder1_validateImg", "輸入的值與圖片中的不符" }
        };
        foreach (var errorSpan in styleBasedErrorSpans)
        {
            var spanNode = resultDoc.GetElementbyId(errorSpan.Key);
            if (spanNode != null)
            {
                var style = spanNode.GetAttributeValue("style", "");
                if (!style.Contains("display:none") && !style.Contains("visibility:hidden"))
                {
                    var errorMessage = spanNode.InnerText.Trim();
                    _logger.LogWarning("Query failed: A visible style-based error span was found. ID: {SpanId}, Message: {ErrorMessage}", errorSpan.Key, errorMessage);
                    return new QueryResult(false, string.IsNullOrEmpty(errorMessage) ? errorSpan.Value : errorMessage, resultHtml);
                }
            }
        }

        var textBasedErrorSpans = new[]
        {
            "ctl00_ContentPlaceHolder1_validateInputS",
            "ctl00_ContentPlaceHolder1_txtInputSError",
            "ctl00_ContentPlaceHolder1_labBirthError"
        };
        foreach (var spanId in textBasedErrorSpans)
        {
            var spanNode = resultDoc.GetElementbyId(spanId);
            if (spanNode != null && !string.IsNullOrWhiteSpace(spanNode.InnerText))
            {
                var errorMessage = spanNode.InnerText.Trim();
                _logger.LogWarning("Query failed: An InnerText-based error span was found. ID: {SpanId}, Message: {ErrorMessage}", spanId, errorMessage);
                return new QueryResult(false, errorMessage, resultHtml);
            }
        }

        var noDataPanel = resultDoc.GetElementbyId("ctl00_ContentPlaceHolder1_panelFailResult");
        if (noDataPanel != null)
        {
            var noDataMessage = noDataPanel.SelectSingleNode(".//strong")?.InnerText.Trim() ?? "目前查無您的掛號資料!";
            _logger.LogWarning("Query failed: The 'No Data Found' panel was displayed. Message: {Message}", noDataMessage);
            return new QueryResult(false, noDataMessage, resultHtml);
        }

        _logger.LogError("Query failed: Could not determine the result from the response HTML. It's not a known success or failure pattern.");
        return new QueryResult(false, "未知的回應格式", resultHtml);
    }
}
