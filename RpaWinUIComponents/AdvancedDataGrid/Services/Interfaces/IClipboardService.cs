using System.Threading.Tasks;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;

/// <summary>
/// Service for handling clipboard operations with Excel compatibility
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Gets clipboard data as text
    /// </summary>
    Task<string> GetClipboardDataAsync();

    /// <summary>
    /// Sets clipboard data with multiple formats
    /// </summary>
    Task SetClipboardDataAsync(string data);

    /// <summary>
    /// Checks if clipboard contains text data
    /// </summary>
    Task<bool> HasClipboardDataAsync();

    /// <summary>
    /// Converts 2D array to Excel-compatible tab-delimited format
    /// </summary>
    string ConvertToExcelFormat(string[,] data);

    /// <summary>
    /// Parses Excel-compatible tab-delimited format to 2D array
    /// </summary>
    string[,] ParseFromExcelFormat(string clipboardData);

    /// <summary>
    /// Copies selected cells to clipboard with multiple formats
    /// </summary>
    Task CopySelectedCellsAsync(string[,] selectedData);

    /// <summary>
    /// Gets clipboard data in structured format
    /// </summary>
    Task<string[,]> PasteStructuredDataAsync();
}