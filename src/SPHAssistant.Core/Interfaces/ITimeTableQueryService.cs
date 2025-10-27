using SPHAssistant.Core.Models.TimeTable;

namespace SPHAssistant.Core.Interfaces;

/// <summary>
/// Defines the contract for a service that queries and filters timetable data.
/// </summary>
public interface ITimeTableQueryService
{
    /// <summary>
    /// Finds all appointment slots for a specific doctor, optionally within a date range and filtered by availability.
    /// </summary>
    /// <param name="timeTable">The complete timetable for a department.</param>
    /// <param name="doctorName">The name of the doctor to search for.</param>
    /// <param name="startDate">The optional start date for the search range.</param>
    /// <param name="endDate">The optional end date for the search range.</param>
    /// <param name="onlyAvailable">If true, only returns slots that are currently available for booking.</param>
    /// <returns>A dictionary where the key is the date and the value is a list of matching appointment slots for that doctor on that day.</returns>
    Dictionary<DateOnly, List<AppointmentSlot>> FindSlotsByDoctor(
        DepartmentTimeTable timeTable,
        string doctorName,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool onlyAvailable = true);

    /// <summary>
    /// Finds all daily schedules within a specified date range, optionally filtering for only available slots.
    /// </summary>
    /// <param name="timeTable">The complete timetable for a department.</param>
    /// <param name="startDate">The optional start date for the search range. If null, searches from the beginning.</param>
    /// <param name="endDate">The optional end date for the search range. If null, searches to the end.</param>
    /// <param name="onlyAvailable">If true, the returned daily schedules will only contain slots that are available.</param>
    /// <returns>A dictionary where the key is the date and the value is the corresponding <see cref="DailyTimeTable"/>.</returns>
    Dictionary<DateOnly, DailyTimeTable> FindDailySchedulesByDateRange(
        DepartmentTimeTable timeTable,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool onlyAvailable = false);
}
