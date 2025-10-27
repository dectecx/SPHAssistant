using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.DTOs;
using SPHAssistant.Core.Models.Enums;
using SPHAssistant.Core.Models.Result;
using SPHAssistant.Core.Models.Data;
using SPHAssistant.Core.Models.TimeTable;
using System.Text.RegularExpressions;
using SPHAssistant.Core.Models.Booking;
using SPHAssistant.Core.Models.Internal;

namespace SPHAssistant.Core.Services;

/// <summary>
/// Implements the client for interacting with the St. Paul's Hospital website.
/// </summary>
public class HospitalClient : IHospitalClient
{
    private readonly ILogger<HospitalClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly IOcrService _ocrService;
    private static readonly Random _random = new();

    /// <summary>
    /// The URL of the query page.
    /// </summary>
    private const string QueryPageUrl = "Query.aspx?loc=S";

    /// <summary>
    /// The URL of the captcha page.
    /// </summary>
    private const string CaptchaUrl = "ValidateCode.aspx";

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
    public HospitalClient(ILogger<HospitalClient> logger, HttpClient httpClient, IOcrService ocrService)
    {
        _logger = logger;
        _ocrService = ocrService;
        _httpClient = httpClient;

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
                var captchaText = await RecognizeCaptchaAsync();
                if (string.IsNullOrEmpty(captchaText) || captchaText.Length != 4)
                {
                    _logger.LogWarning("Captcha attempt {Attempt} failed: The captcha is not recognized or the length is not 4. Captcha text: {CaptchaText}. Retrying in 1 second...", attempt, captchaText);
                    await Task.Delay(1000);
                    continue;
                }

                // Step 3: Post the form.
                var resultHtml = await PostQueryFormAsync(request, webFormsState, captchaText);
                if (string.IsNullOrEmpty(resultHtml))
                {
                    return new OperationError("Failed to get a response after posting the query form.");
                }

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

    /// <summary>
    /// Fetches the web forms state from the initial query page.
    /// </summary>
    /// <returns>The web forms state.</returns>
    private async Task<WebFormsState?> FetchWebFormsStateAsync()
    {
        _logger.LogInformation("Fetching initial query page to get ViewState and cookies.");

        var response = await _httpClient.GetAsync(QueryPageUrl);
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
    /// Posts the query form to the hospital website.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="state">The web forms state.</param>
    /// <param name="captchaText">The recognized captcha text.</param>
    /// <returns>The response HTML.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the query type is invalid.</exception>
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

        var postRequest = new HttpRequestMessage(HttpMethod.Post, QueryPageUrl)
        {
            Content = new FormUrlEncodedContent(formData)
        };
        var postResponse = await _httpClient.SendAsync(postRequest);
        postResponse.EnsureSuccessStatusCode();
        return await postResponse.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Analyzes the response HTML for success or specific error indicators.
    /// </summary>
    /// <param name="html">The response HTML.</param>
    /// <returns>The query status.</returns>
    private QueryStatus AnalyzeResponseHtml(string html)
    {
        _logger.LogInformation("Analyzing response HTML for success or specific error indicators.");
        var resultDoc = new HtmlDocument();
        resultDoc.LoadHtml(html);

        // Priority 1: Check for the success table.
        var successTable = resultDoc.GetElementbyId("ctl00_ContentPlaceHolder1_gvQueryResult");
        if (successTable != null)
        {
            _logger.LogInformation("Query successful: Found the result table 'gvQueryResult'.");
            return new QuerySuccess(html);
        }

        // Priority 2: Check against the structured error definitions.
        foreach (var definition in _errorDefinitions)
        {
            var spanNode = resultDoc.GetElementbyId(definition.Id);
            if (spanNode == null)
            {
                continue;
            }

            bool isError;
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
                return definition.CreateStatus(errorMessage, html);
            }
        }

        // Priority 3: Check for the "No Data Found" panel.
        var noDataPanel = resultDoc.GetElementbyId("ctl00_ContentPlaceHolder1_panelFailResult");
        if (noDataPanel != null)
        {
            var noDataMessage = noDataPanel.SelectSingleNode(".//strong")?.InnerText.Trim() ?? "目前查無您的掛號資料!";
            _logger.LogWarning("Query failed: The 'No Data Found' panel was displayed. Message: {Message}", noDataMessage);
            return new DataNotFound(noDataMessage, html);
        }

        _logger.LogError("Query failed: Could not determine the result from the response HTML. It's not a known success or failure pattern.");
        return new UnknownResponse("未知的回應格式", html);
    }

    /// <inheritdoc/>
    public TableData ParseAppointmentData(string successHtml)
    {
        var tableData = new TableData
        {
            Headers = [],
            Rows = []
        };
        var doc = new HtmlDocument();
        doc.LoadHtml(successHtml);

        var tableNode = doc.GetElementbyId("ctl00_ContentPlaceHolder1_gvQueryResult");
        if (tableNode == null)
        {
            _logger.LogWarning("Could not find the appointment result table ('gvQueryResult') in the provided HTML.");
            return tableData;
        }

        // --- Extract Headers ---
        var headerNodes = tableNode.SelectNodes(".//th");
        if (headerNodes != null)
        {
            tableData.Headers = headerNodes.Select(th => th.InnerText.Trim()).ToList();
        }

        // --- Extract Rows ---
        // We select the table body's rows, skipping the first row which is the header.
        var rowNodes = tableNode.SelectNodes(".//tr[position()>1]");
        if (rowNodes != null)
        {
            foreach (var rowNode in rowNodes)
            {
                var row = rowNode
                    .SelectNodes(".//td")
                    .Select(td => td.InnerText.Trim())
                    .ToList();
                tableData.Rows.Add(row);
            }
        }

        _logger.LogInformation("Successfully parsed {RowCount} rows from the appointment table.", tableData.Rows.Count);
        return tableData;
    }

    /// <inheritdoc/>
    public async Task<DepartmentTimeTable?> GetTimeTableAsync(string departmentCode)
    {
        _logger.LogInformation("Fetching timetable for department code: {DepartmentCode}", departmentCode);
        try
        {
            var url = $"RMSTimeTable.aspx?dpt={departmentCode}";
            var html = await _httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Get department name from our static data service instead of parsing the page.
            var departmentName = DepartmentDataService.GetDepartmentName(departmentCode);

            var timeTable = new DepartmentTimeTable
            {
                DepartmentCode = departmentCode,
                DepartmentName = departmentName
            };

            // The main content table is nested. A reliable selector is to find the one with the specific "tableStyle" class.
            var tableNode = doc.DocumentNode.SelectSingleNode("//table[@class='tableStyle']");
            if (tableNode == null)
            {
                _logger.LogWarning("Timetable table 'gvDpt' not found for department {DepartmentCode}.", departmentCode);
                return timeTable;
            }

            // Select all rows with a specific border style, skipping the first empty header row.
            var rowNodes = tableNode.SelectNodes(".//tr[contains(@style, 'border-color:#d4d0c8') and position()>1]");
            if (rowNodes == null)
            {
                return timeTable;
            }

            foreach (var rowNode in rowNodes)
            {
                var cells = rowNode.SelectNodes(".//td");
                if (cells == null || cells.Count < 4)
                {
                    // Expect at least 4 cells: Date, Morning, Afternoon, Night
                    continue;
                }

                var dailyTimeTable = new DailyTimeTable();

                // Cell 0: Date. The format is "YYYY年MM月DD日<br>(...)"
                var dateHtml = cells[0].InnerHtml;
                var datePart = dateHtml.Split(new[] { "<br>" }, StringSplitOptions.None)[0];
                var parsableDateString = datePart.Replace("年", "/").Replace("月", "/").Replace("日", "").Trim();
                if (!DateOnly.TryParse(parsableDateString, out var date))
                {
                    continue; // Skip row if date is invalid
                }
                dailyTimeTable.Date = date;

                // Cell 1: Morning Slots
                dailyTimeTable.MorningSlots = ParseSlotsFromCell(cells[1]);

                // Cell 2: Afternoon Slots
                dailyTimeTable.AfternoonSlots = ParseSlotsFromCell(cells[2]);

                // Cell 3: Night Slots
                dailyTimeTable.NightSlots = ParseSlotsFromCell(cells[3]);

                timeTable.DailyTimeTables.Add(dailyTimeTable);
            }

            return timeTable;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch timetable for department {DepartmentCode} due to an HTTP error.", departmentCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while parsing the timetable for department {DepartmentCode}.", departmentCode);
            return null;
        }
    }

    /// <summary>
    /// Parses the slots from a cell.
    /// </summary>
    /// <param name="cell">The cell to parse.</param>
    /// <returns>The slots.</returns>
    private List<AppointmentSlot> ParseSlotsFromCell(HtmlNode cell)
    {
        var slots = new List<AppointmentSlot>();
        var innerHtml = cell.InnerHtml.Trim();

        // If the cell is empty or just a non-breaking space, there are no clinics.
        if (string.IsNullOrEmpty(innerHtml) || innerHtml == "&nbsp;")
        {
            return slots;
        }

        // Each doctor's info is separated by a <br> tag.
        var doctorHtmlChunks = innerHtml.Split(new[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var chunk in doctorHtmlChunks)
        {
            var chunkDoc = new HtmlDocument();
            chunkDoc.LoadHtml(chunk);

            var spanNode = chunkDoc.DocumentNode.SelectSingleNode("//span");
            var aNode = chunkDoc.DocumentNode.SelectSingleNode("//a");

            string rawText;
            SlotStatus status;
            BookingParameters? bookingParams = null;

            if (spanNode != null)
            {
                // Case 1: Status is indicated in a <span> (e.g., Full, NoClinic)
                rawText = System.Net.WebUtility.HtmlDecode(spanNode.InnerText.Trim());
                if (rawText.Contains("(額滿)"))
                {
                    status = SlotStatus.Full;
                }
                else if (rawText.Contains("(停診)"))
                {
                    status = SlotStatus.NoClinic;
                }
                else
                {
                    // Should not happen often
                    status = SlotStatus.Unknown;
                }
            }
            else if (aNode != null)
            {
                // Case 2: An <a> tag is present, indicating an available slot.
                rawText = System.Net.WebUtility.HtmlDecode(aNode.InnerText.Trim());
                status = SlotStatus.Available;

                // Attempt to parse the booking parameters from the href attribute.
                var hrefAttribute = aNode.GetAttributeValue("href", "");
                // First, decode any HTML entities in the URL string, like &amp; -> &
                var href = System.Net.WebUtility.HtmlDecode(hrefAttribute);

                if (!string.IsNullOrEmpty(href) && href.StartsWith("Login.aspx"))
                {
                    try
                    {
                        var queryIndex = href.IndexOf('?');
                        if (queryIndex == -1 || queryIndex == href.Length - 1)
                        {
                            _logger.LogWarning("Malformed booking URL found (missing or empty query string): {Href}", href);
                        }
                        else
                        {
                            var queryString = href.Substring(queryIndex + 1);
                            var queryParams = queryString.Split('&')
                                .Select(p => p.Split('='))
                                .Where(p => p.Length == 2)
                                .ToDictionary(p => p[0], p => System.Net.WebUtility.UrlDecode(p[1]));

                            if (queryParams.TryGetValue("rmsData", out var rmsData) &&
                                queryParams.TryGetValue("dptName", out var dptName) &&
                                queryParams.TryGetValue("dpt", out var dpt) &&
                                queryParams.TryGetValue("dptDptuid", out var dptDptuid))
                            {
                                bookingParams = new BookingParameters(rmsData, dptName, dpt, dptDptuid);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse booking parameters from href: {Href}", href);
                    }
                }
            }
            else
            {
                // Skip if the chunk contains neither a span nor a link
                continue;
            }

            // Extract ID and Name from the start of the string
            var nameAndIdText = Regex.Replace(rawText, @"\s*\([\s\S]*\)", "").Trim();
            var match = Regex.Match(nameAndIdText, @"^(\d*)(.*)");

            if (match.Success)
            {
                var doctorId = match.Groups[1].Value;
                var doctorName = match.Groups[2].Value.Trim();

                // Handle cases where ID might be part of the doctor name in <a> tag but not span
                if (string.IsNullOrEmpty(doctorId) && aNode != null)
                {
                    var aText = System.Net.WebUtility.HtmlDecode(aNode.InnerText.Trim());
                    var aMatch = Regex.Match(aText, @"^(\d+)(.*)");
                    if (aMatch.Success)
                    {
                        doctorId = aMatch.Groups[1].Value;
                        doctorName = aMatch.Groups[2].Value.Trim();
                    }
                }

                slots.Add(new AppointmentSlot(new Doctor(doctorId, doctorName), status, rawText, bookingParams));
            }
        }
        return slots;
    }
}
