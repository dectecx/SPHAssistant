namespace SPHAssistant.Core.Models.Enums;

/// <summary>
/// Represents the type of identification used for the query.
/// </summary>
public enum IdType
{
    /// <summary>
    /// National Identification Number. Corresponds to value "0".
    /// </summary>
    IdCard = 0,

    /// <summary>
    /// Hospital's medical record number. Corresponds to value "1".
    /// </summary>
    MedicalRecord = 1,

    /// <summary>
    /// Resident certificate number. Corresponds to value "3".
    /// </summary>
    ResidentCertificate = 3
}
