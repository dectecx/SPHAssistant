using SPHAssistant.Core.Models.Enums;

namespace SPHAssistant.Core.Models.DTOs;

/// <summary>
/// Represents the necessary data for making an appointment query.
/// </summary>
/// <param name="QueryType">The type of query (returning or new patient).</param>
/// <param name="IdType">The type of identification being used.</param>
/// <param name="IdNumber">The identification number (e.g., ID card number).</param>
/// <param name="BirthDate">The patient's birth date in MMDD format (e.g., "1125").</param>
public record QueryRequest(
    QueryType QueryType,
    IdType IdType,
    string IdNumber,
    string BirthDate
);
