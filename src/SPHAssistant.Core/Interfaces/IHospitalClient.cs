using SPHAssistant.Core.Models.Data;
using SPHAssistant.Core.Models.DTOs;
using SPHAssistant.Core.Models.Result;
using SPHAssistant.Core.Models.TimeTable;
using System.Threading.Tasks;

namespace SPHAssistant.Core.Interfaces;

/// <summary>
/// Defines the contract for a client that interacts with the St. Paul's Hospital website.
/// </summary>
public interface IHospitalClient
{
    /// <summary>
    /// Asynchronously performs an appointment query on the hospital's website.
    /// </summary>
    /// <param name="request">The data required for the query.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a QueryStatus indicating the outcome.</returns>
    Task<QueryStatus> QueryAppointmentAsync(QueryRequest request);

    /// <summary>
    /// Parses the HTML content of a successful query response to extract appointment data.
    /// </summary>
    /// <param name="successHtml">The full HTML content of the page containing the result table.</param>
    /// <returns>A TableData object containing the structured appointment information.</returns>
    TableData ParseAppointmentData(string successHtml);

    /// <summary>
    /// Asynchronously fetches and parses the timetable for a specific department.
    /// </summary>
    /// <param name="departmentCode">The code of the department to query (e.g., "S2700A" for Orthopedics).</param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains a DepartmentTimeTable object with the parsed schedule,
    /// or null if the operation fails.
    /// </returns>
    Task<DepartmentTimeTable?> GetTimeTableAsync(string departmentCode);
}
