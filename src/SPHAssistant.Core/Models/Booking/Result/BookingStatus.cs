namespace SPHAssistant.Core.Models.Booking.Result;

/// <summary>
/// Represents the abstract base for a booking result status.
/// </summary>
public abstract record BookingStatus(string Message, string? ResponseHtml = null);

/// <summary>
/// Represents a successful booking.
/// </summary>
public sealed record BookingSuccess(string Message, string ResponseHtml)
    : BookingStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure due to an incorrect or expired captcha.
/// </summary>
public sealed record BookingCaptchaError(string Message, string ResponseHtml)
    : BookingStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure due to invalid patient data (e.g., ID or birth date format error).
/// </summary>
public sealed record BookingValidationError(string Message, string ResponseHtml)
    : BookingStatus(Message, ResponseHtml);
    
/// <summary>
/// Represents a failure where the slot is already full or no longer available.
/// </summary>
public sealed record SlotUnavailableError(string Message, string ResponseHtml)
    : BookingStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure due to an unknown or unhandled response pattern from the server.
/// </summary>
public sealed record UnknownBookingResponse(string Message, string ResponseHtml)
    : BookingStatus(Message, ResponseHtml);

/// <summary>
/// Represents a failure due to an HTTP or other unexpected exception.
/// </summary>
public sealed record BookingOperationError(string Message) 
    : BookingStatus(Message);
