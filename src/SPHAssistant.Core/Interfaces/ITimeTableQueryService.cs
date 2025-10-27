using SPHAssistant.Core.Models.TimeTable;

namespace SPHAssistant.Core.Interfaces;

/// <summary>
/// Defines the contract for a service that queries and filters timetable data.
/// </summary>
public interface ITimeTableQueryService
{
    /// <summary>
    /// Finds all available appointment slots for a specific doctor from a given department timetable.
    /// </summary>
    /// <param name="timeTable">The complete timetable for a department.</param>
    /// <param name="doctorName">The name of the doctor to search for.</param>
    /// <param name="onlyAvailable">If true, only returns slots that are currently available for booking.</param>
    /// <returns>A dictionary where the key is the date and the value is a list of appointment slots for that doctor on that day.</returns>
    Dictionary<DateOnly, List<AppointmentSlot>> FindSlotsByDoctor(DepartmentTimeTable timeTable, string doctorName, bool onlyAvailable = true);
}
