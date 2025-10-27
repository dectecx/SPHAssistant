using SPHAssistant.Core.Models.Enums;

namespace SPHAssistant.Core.Models.Booking;

/// <summary>
/// Represents all the data required to make a booking attempt.
/// </summary>
/// <param name="Parameters">The parameters extracted from the timetable link for the specific slot.</param>
/// <param name="IdType">The type of identification being used.</param>
/// <param name="IdNumber">The identification number (e.g., ID card number).</param>
/// <param name="BirthDate">The patient's birth date in MMDD format (e.g., "1125").</param>
/// <param name="IsFirstVisit">Indicates if this is the patient's first visit to this department.</param>
public record BookingRequest(
    BookingParameters Parameters,
    IdType IdType,
    string IdNumber,
    string BirthDate,
    bool IsFirstVisit
);
