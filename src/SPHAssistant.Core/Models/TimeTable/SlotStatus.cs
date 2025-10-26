namespace SPHAssistant.Core.Models.TimeTable;

/// <summary>
/// Represents the status of an appointment slot.
/// </summary>
public enum SlotStatus
{
    /// <summary>
    /// The status is unknown or could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// The slot is available for registration.
    /// </summary>
    Available,

    /// <summary>
    /// The slot is full and no longer accepting registrations.
    /// </summary>
    Full,

    /// <summary>
    /// The clinic is not in session for this slot.
    /// </summary>
    NoClinic
}
