namespace SPHAssistant.Core.Models.TimeTable;

/// <summary>
/// Represents a single appointment slot for a specific doctor.
/// </summary>
/// <param name="Doctor">The doctor for this slot.</param>
/// <param name="Status">The current status of the slot (e.g., Available, Full).</param>
/// <param name="RawText">The original raw text content from the HTML cell.</param>
public record AppointmentSlot(Doctor Doctor, SlotStatus Status, string RawText);
