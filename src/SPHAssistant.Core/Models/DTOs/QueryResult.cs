namespace SPHAssistant.Core.Models.DTOs;

/// <summary>
/// Represents the result of an appointment query.
/// </summary>
/// <param name="IsSuccess">Indicates whether the query was successful.</param>
/// <param name="Message">A message describing the result (e.g., an error message or success confirmation).</param>
/// <param name="ResponseHtml">The full HTML content of the response page.</param>
public record QueryResult(
    bool IsSuccess,
    string Message,
    string? ResponseHtml = null
);
