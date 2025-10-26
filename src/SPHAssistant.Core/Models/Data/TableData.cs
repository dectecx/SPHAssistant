namespace SPHAssistant.Core.Models.Data;

/// <summary>
/// Represents the data structure for a two-dimensional table.
/// </summary>
public class TableData
{
    /// <summary>
    /// Gets or sets the list of header strings for the table columns.
    /// </summary>
    public required List<string> Headers { get; set; }

    /// <summary>
    /// Gets or sets the list of rows. Each row is a list of strings representing the cell data.
    /// </summary>
    public required List<List<string>> Rows { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableData"/> class.
    /// </summary>
    public TableData()
    {
        Headers = new List<string>();
        Rows = new List<List<string>>();
    }
}
