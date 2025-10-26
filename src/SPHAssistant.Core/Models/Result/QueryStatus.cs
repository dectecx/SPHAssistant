namespace SPHAssistant.Core.Models.Result;

/// <summary>
/// Represents the abstract base for a query result status.
/// </summary>
public abstract record QueryStatus(string Message, string? ResponseHtml = null);

/// <summary>
/// Represents a successful query, containing the appointment data table.
/// </summary>
public sealed record QuerySuccess(string ResponseHtml) 
    : QueryStatus("查詢成功", ResponseHtml);

/// <summary>
/// Represents a failure due to an incorrect or expired captcha.
/// </summary>
public sealed record CaptchaError(string Message, string ResponseHtml) 
    : QueryStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure where no matching appointment data was found.
/// </summary>
public sealed record DataNotFound(string Message, string ResponseHtml) 
    : QueryStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure due to invalid input data (e.g., birth date format).
/// </summary>
public sealed record ValidationError(string Message, string ResponseHtml) 
    : QueryStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure due to an unknown or unhandled response pattern from the server.
/// </summary>
public sealed record UnknownResponse(string Message, string ResponseHtml) 
    : QueryStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure due to an HTTP or other unexpected exception.
/// </summary>
public sealed record OperationError(string Message) 
    : QueryStatus(Message);
