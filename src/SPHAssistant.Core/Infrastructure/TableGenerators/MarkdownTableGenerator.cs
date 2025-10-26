using SPHAssistant.Core.Interfaces;
using SPHAssistant.Core.Models.Data;
using System.Text;

namespace SPHAssistant.Core.Infrastructure.TableGenerators;

/// <summary>
/// Implements a table generator that outputs in Markdown format.
/// </summary>
public class MarkdownTableGenerator : ITableGenerator
{
    /// <inheritdoc/>
    public string Generate(TableData tableData)
    {
        if (tableData.Headers == null || !tableData.Headers.Any())
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        // --- Header ---
        sb.Append("| ");
        foreach (var header in tableData.Headers)
        {
            sb.Append(header).Append(" | ");
        }
        sb.AppendLine();

        // --- Separator ---
        sb.Append("|");
        foreach (var _ in tableData.Headers)
        {
            sb.Append("---|");
        }
        sb.AppendLine();

        // --- Rows ---
        if (tableData.Rows != null)
        {
            foreach (var row in tableData.Rows)
            {
                sb.Append("| ");
                for (int i = 0; i < tableData.Headers.Count; i++)
                {
                    // Ensure row has enough cells, provide empty string if not
                    var cellValue = (i < row.Count) ? row[i] : string.Empty;
                    sb.Append(cellValue).Append(" | ");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
