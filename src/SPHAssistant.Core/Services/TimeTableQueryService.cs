using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.TimeTable;

namespace SPHAssistant.Core.Services;

/// <summary>
/// Implements the service for querying and filtering timetable data.
/// </summary>
public class TimeTableQueryService : ITimeTableQueryService
{
    /// <inheritdoc/>
    public Dictionary<DateOnly, List<AppointmentSlot>> FindSlotsByDoctor(DepartmentTimeTable timeTable, string doctorName, bool onlyAvailable = true)
    {
        var results = new Dictionary<DateOnly, List<AppointmentSlot>>();

        foreach (var dailyTimeTable in timeTable.DailyTimeTables)
        {
            // Combine all slots from morning, afternoon, and night for the current day.
            var allSlotsForDay = dailyTimeTable.MorningSlots
                .Concat(dailyTimeTable.AfternoonSlots)
                .Concat(dailyTimeTable.NightSlots);

            // Filter the slots based on the doctor's name and availability.
            var filteredSlots = allSlotsForDay
                .Where(slot => slot.Doctor.Name.Contains(doctorName))
                .Where(slot => !onlyAvailable || slot.Status == SlotStatus.Available)
                .ToList();

            // If any slots are found for the doctor on this day, add them to the results.
            if (filteredSlots.Count > 0)
            {
                results.Add(dailyTimeTable.Date, filteredSlots);
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public DailyTimeTable? FindSlotsByDate(DepartmentTimeTable timeTable, DateOnly date)
    {
        return timeTable.DailyTimeTables.FirstOrDefault(daily => daily.Date == date);
    }
}
