namespace SPHAssistant.Core.Models.Booking;

/// <summary>
/// Represents the parameters required to initiate a booking process.
/// These are typically extracted from the 'href' of an available slot's link.
/// </summary>
/// <param name="RmsData">The core encrypted data string containing date, doctor, and session info. Example: "20251027PM1S2700N1946洪偉翔"</param>
/// <param name="DptName">The department name parameter, typically a fixed value. Example: "NNNN"</param>
/// <param name="Dpt">The department code. Example: "S2700A"</param>
/// <param name="DptDptuid">The unique department and patient type identifier. Example: "S2700AA"</param>
public record BookingParameters(
    string RmsData,
    string DptName,
    string Dpt,
    string DptDptuid);
