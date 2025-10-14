namespace SPHAssistant.Core.Models.Enums;

/// <summary>
/// Represents the type of query for an appointment.
/// </summary>
public enum QueryType
{
    /// <summary>
    /// For returning patients. Corresponds to value "rbnSeveralTimes".
    /// </summary>
    ReturningPatient,

    /// <summary>
    /// For new patients. Corresponds to value "rbnFirstTime".
    /// </summary>
    NewPatient
}
