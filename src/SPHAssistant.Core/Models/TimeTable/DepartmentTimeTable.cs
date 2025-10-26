namespace SPHAssistant.Core.Models.TimeTable;

/// <summary>
/// Represents the entire timetable for a specific department.
/// </summary>
public class DepartmentTimeTable
{
    /// <summary>
    /// The name of the department (e.g., "神經內科").
    /// </summary>
    public required string DepartmentName { get; set; }

    /// <summary>
    /// The code of the department (e.g., "S3700A").
    /// </summary>
    public required string DepartmentCode { get; set; }

    /// <summary>
    /// The list of daily timetables for this department.
    /// </summary>
    public List<DailyTimeTable> DailyTimeTables { get; set; } = new();
}
