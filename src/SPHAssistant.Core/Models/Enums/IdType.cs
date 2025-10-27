namespace SPHAssistant.Core.Models.Enums;

/// <summary>
/// Represents the type of identification used for the query.
/// </summary>
public enum IdType
{
    /// <summary>
    /// National Identification Number. Corresponds to value "0".
    /// 身分證字號
    /// </summary>
    IdCard = 0,

    /// <summary>
    /// Hospital's medical record number. Corresponds to value "1".
    /// 病歷號
    /// </summary>
    MedicalRecord = 1,

    /// <summary>
    /// Passport number. Corresponds to value "2".
    /// 護照號碼
    /// </summary>
    Passport = 2,

    /// <summary>
    /// Resident certificate number. Corresponds to value "3".
    /// 居留證號
    /// </summary>
    ResidentCertificate = 3,

    /// <summary>
    /// Entry and exit permit number. Corresponds to value "4".
    /// 入出境許可證號
    /// </summary>
    EntryExitPermit = 4
}
