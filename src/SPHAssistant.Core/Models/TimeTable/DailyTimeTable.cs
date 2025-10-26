namespace SPHAssistant.Core.Models.TimeTable;

/// <summary>
/// Represents the clinic timetable for a single day.
/// </summary>
public class DailyTimeTable
{
    /// <summary>
    /// The specific date for this timetable.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The list of appointment slots for the morning session.
    /// </summary>
    public List<AppointmentSlot> MorningSlots { get; set; } = new();

    /// <summary>
    /// The list of appointment slots for the afternoon session.
    /// </summary>
    public List<AppointmentSlot> AfternoonSlots { get; set; } = new();

    /// <summary>
    /// The list of appointment slots for the night session.
    /// </summary>
    public List<AppointmentSlot> NightSlots { get; set; } = new();
}
