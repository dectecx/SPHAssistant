namespace SPHAssistant.Core.Services;

/// <summary>
/// Provides a static repository for department information.
/// </summary>
public static class DepartmentDataService
{
    private static readonly Dictionary<string, (string Category, string Name)> _departmentMap = new()
    {
        // Internal Medicine
        { "S1100A", ("內科", "一般內科") },
        { "S1500A", ("內科", "腎臟科") },
        { "S1600A", ("內科", "新陳代謝科") },
        { "S1300A", ("內科", "胸腔內科") },
        { "S1100D", ("內科", "職業病門診") },
        { "S3H00A", ("內科", "家庭醫學科") },
        { "S1700A", ("內科", "心臟內科") },
        { "S1800A", ("內科", "風濕免疫過敏科") },
        { "S3700A", ("內科", "神經內科") },
        { "S1A00A", ("內科", "感染科") },
        { "S3700C", ("內科", "肉毒桿菌素注射特別門診") },
        { "S1200A", ("內科", "腸胃肝膽科") },
        { "S1700C", ("內科", "周邊血管門診") },
        { "S1400A", ("內科", "腫瘤內科") },
        { "S1700ZS1800ZS1600ZS3700Z", ("內科", "多重慢性併整合門診") },
        { "S1700ES1300ES3H00E", ("內科", "戒菸門診") },
        { "S0990G", ("內科", "COVID-19自費採檢") },
        // Surgery
        { "S2100A", ("外科", "一般外科") },
        { "S2300A", ("外科", "腦神經脊椎外科") },
        { "S2500A", ("外科", "整形外科") },
        { "S2600A", ("外科", "泌尿外科") },
        { "S2700A", ("外科", "骨科") },
        { "S2300CS2700C", ("外科", "下背痛門診") },
        { "S2700D", ("外科", "運動傷害門診") },
        { "S2200B", ("外科", "周邊血管門診") },
        { "S2200A", ("外科", "胸腔暨心臟血管外科") },
        { "S2700JS3H00JS2300J", ("外科", "骨質疏鬆門診") },
        // Dentistry
        { "SD000ASD140B", ("牙科", "一般牙科") },
        // Obstetrics/Gynecology
        { "S7000A", ("婦產科/女性門診", "婦產科") },
        // Pediatrics
        { "S3200A", ("兒童專科", "兒科") },
        { "S3210A", ("兒童專科", "健兒門診") },
        { "S3200K", ("兒童專科", "兒童發展篩檢門診") },
        // Other Specialties
        { "S3400A", ("其他專科", "眼科") },
        { "S3500A", ("其他專科", "耳鼻喉科") },
        { "S3K00A", ("其他專科", "營養諮詢門診") },
        { "S3800A", ("其他專科", "皮膚科") },
        { "S3900A", ("其他專科", "復健科") },
        { "S1950A", ("其他專科", "健康檢查科") },
        { "S3600A", ("其他專科", "精神科(限十八歲以上者)") },
        { "S3600B", ("其他專科", "兒童青少年心智健康門診") },
        { "S3H00V", ("其他專科", "健康減重門診") },
    };

    /// <summary>
    /// Gets the name of the department for a given department code.
    /// </summary>
    /// <param name="code">The department code.</param>
    /// <returns>The department name, or "Unknown" if the code is not found.</returns>
    public static string GetDepartmentName(string code)
    {
        return _departmentMap.TryGetValue(code, out var info) ? info.Name : "Unknown";
    }
}
