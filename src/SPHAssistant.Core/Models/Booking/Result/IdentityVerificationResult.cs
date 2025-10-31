using SPHAssistant.Core.Models.Internal;

namespace SPHAssistant.Core.Models.Booking.Result;

/// <summary>
/// Represents the result of the initial identity verification step.
/// </summary>
public abstract record IdentityVerificationResult;

/// <summary>
/// Indicates that the patient is a returning patient and further information is required on the confirmation page.
/// </summary>
/// <param name="Html">The HTML content of the confirmation page.</param>
/// <param name="State">The WebForms state extracted from the confirmation page.</param>
public sealed record ConfirmationRequired(string Html, WebFormsState State) : IdentityVerificationResult;

/// <summary>
/// Indicates that the patient is a new patient and registration is required. This flow is not yet implemented.
/// </summary>
/// <param name="State">The WebForms state extracted from the new patient page.</param>
public sealed record NewPatientRegistrationRequired(WebFormsState State) : IdentityVerificationResult;

/// <summary>
/// Indicates that the identity verification step failed with a definitive booking status.
/// </summary>
/// <param name="Status">The specific failure status (e.g., CaptchaError, SlotUnavailableError).</param>
public sealed record VerificationFailed(BookingStatus Status) : IdentityVerificationResult;
