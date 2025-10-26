using SPHAssistant.Core.Models.Data;

namespace SPHAssistant.Core.Interfaces;

/// <summary>
/// Defines the contract for a service that generates a string representation of a table.
/// </summary>
public interface ITableGenerator
{
    /// <summary>
    /// Generates a table based on the provided data.
    /// </summary>
    /// <param name="tableData">The data to be formatted into a table.</param>
    /// <returns>A string representation of the table.</returns>
    string Generate(TableData tableData);
}
