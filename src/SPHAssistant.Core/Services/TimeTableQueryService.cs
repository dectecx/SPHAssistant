using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.TimeTable;

namespace SPHAssistant.Core.Services;

/// <summary>
/// Implements the service for querying and filtering timetable data.
/// </summary>
public class TimeTableQueryService : ITimeTableQueryService
{
    /// <inheritdoc/>
    public Dictionary<DateOnly, List<AppointmentSlot>> FindSlotsByDoctor(
        DepartmentTimeTable timeTable,
        string doctorName,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool onlyAvailable = true)
    {
        var results = new Dictionary<DateOnly, List<AppointmentSlot>>();

        // Establish the date range to iterate through.
        var dailySchedules = GetFilteredDateRange(timeTable, startDate, endDate);

        foreach (var dailyTimeTable in dailySchedules)
        {
            var allSlotsForDay = dailyTimeTable.MorningSlots
                .Concat(dailyTimeTable.AfternoonSlots)
                .Concat(dailyTimeTable.NightSlots);

            var filteredSlots = allSlotsForDay
                .Where(slot => slot.Doctor.Name.Contains(doctorName))
                .Where(slot => !onlyAvailable || slot.Status == SlotStatus.Available)
                .ToList();

            if (filteredSlots.Count > 0)
            {
                results.Add(dailyTimeTable.Date, filteredSlots);
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public Dictionary<DateOnly, DailyTimeTable> FindDailySchedulesByDateRange(
        DepartmentTimeTable timeTable,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool onlyAvailable = false)
    {
        var dailySchedules = GetFilteredDateRange(timeTable, startDate, endDate);

        if (onlyAvailable)
        {
            var filteredResults = new Dictionary<DateOnly, DailyTimeTable>();
            foreach (var daily in dailySchedules)
            {
                var filteredDaily = new DailyTimeTable
                {
                    Date = daily.Date,
                    MorningSlots = daily.MorningSlots.Where(s => s.Status == SlotStatus.Available).ToList(),
                    AfternoonSlots = daily.AfternoonSlots.Where(s => s.Status == SlotStatus.Available).ToList(),
                    NightSlots = daily.NightSlots.Where(s => s.Status == SlotStatus.Available).ToList()
                };

                // Only add the day to results if there is at least one available slot.
                if (filteredDaily.MorningSlots.Count > 0
                    || filteredDaily.AfternoonSlots.Count > 0
                    || filteredDaily.NightSlots.Count > 0)
                {
                    filteredResults.Add(daily.Date, filteredDaily);
                }
            }
            return filteredResults;
        }

        return dailySchedules.ToDictionary(d => d.Date, d => d);
    }

    /// <inheritdoc/>
    public DailyTimeTable? FindSlotsByDate(DepartmentTimeTable timeTable, DateOnly date)
    {
        return timeTable.DailyTimeTables.FirstOrDefault(daily => daily.Date == date);
    }

    /// <inheritdoc/>
    public DailyTimeTable FilterSlotsByStatus(DailyTimeTable dailyTimeTable, SlotStatus status)
    {
        var filteredDailyTable = new DailyTimeTable
        {
            Date = dailyTimeTable.Date,
            MorningSlots = dailyTimeTable.MorningSlots.Where(s => s.Status == status).ToList(),
            AfternoonSlots = dailyTimeTable.AfternoonSlots.Where(s => s.Status == status).ToList(),
            NightSlots = dailyTimeTable.NightSlots.Where(s => s.Status == status).ToList()
        };

        return filteredDailyTable;
    }

    /// <summary>
    /// A private helper to get a sequence of daily timetables based on an optional date range.
    /// </summary>
    private IEnumerable<DailyTimeTable> GetFilteredDateRange(DepartmentTimeTable timeTable, DateOnly? startDate, DateOnly? endDate)
    {
        var sDate = startDate ?? DateOnly.MinValue;
        var eDate = endDate ?? DateOnly.MaxValue;

        return timeTable.DailyTimeTables
            .Where(daily => daily.Date >= sDate && daily.Date <= eDate);
    }
}
