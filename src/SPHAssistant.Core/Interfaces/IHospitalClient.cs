using SPHAssistant.Core.Models.DTOs;

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
    /// <returns>A task that represents the asynchronous operation. The task result contains the query result.</returns>
    Task<QueryResult> QueryAppointmentAsync(QueryRequest request);
}
