using Microsoft.Extensions.Logging;
using RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Implementation;

/// <summary>
/// Implementation of clipboard service for WinUI 3
/// </summary>
public class ClipboardService : IClipboardService
{
    private readonly ILogger<ClipboardService> _logger;

    public ClipboardService(ILogger<ClipboardService>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ClipboardService>.Instance;
    }

    public async Task<string> GetClipboardDataAsync()
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();

            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                var result = await dataPackageView.GetTextAsync();
                _logger.LogDebug("Retrieved clipboard data, length: {Length}", result?.Length ?? 0);
                return result ?? string.Empty;
            }

            _logger.LogDebug("Clipboard does not contain text data");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clipboard data");
            return string.Empty;
        }
    }

    public async Task SetClipboardDataAsync(string data)
    {
        try
        {
            if (string.IsNullOrEmpty(data))
            {
                _logger.LogWarning("SetClipboardDataAsync called with null or empty data");
                return;
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(data);

            // Set HTML format for better compatibility with Excel
            var htmlData = ConvertToHtmlFormat(data);
            dataPackage.SetHtmlFormat(htmlData);

            Clipboard.SetContent(dataPackage);
            _logger.LogDebug("Set clipboard data, length: {Length}", data.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting clipboard data");
        }
    }

    public async Task<bool> HasClipboardDataAsync()
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();
            var result = dataPackageView.Contains(StandardDataFormats.Text);
            _logger.LogDebug("Clipboard contains text: {HasData}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking clipboard data");
            return false;
        }
    }

    public string ConvertToExcelFormat(string[,] data)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                _logger.LogWarning("ConvertToExcelFormat called with null or empty data");
                return string.Empty;
            }

            var sb = new StringBuilder();
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            _logger.LogDebug("Converting {Rows}x{Cols} data to Excel format", rows, cols);

            for (int i = 0; i < rows; i++)
            {
                var rowData = new string[cols];
                for (int j = 0; j < cols; j++)
                {
                    rowData[j] = data[i, j] ?? "";
                }

                if (i > 0)
                    sb.AppendLine();

                sb.Append(string.Join("\t", rowData));
            }

            var result = sb.ToString();
            _logger.LogDebug("Excel format conversion completed, result length: {Length}", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting data to Excel format");
            return string.Empty;
        }
    }

    public string[,] ParseFromExcelFormat(string clipboardData)
    {
        try
        {
            if (string.IsNullOrEmpty(clipboardData))
            {
                _logger.LogWarning("ParseFromExcelFormat called with null or empty data");
                return new string[0, 0];
            }

            _logger.LogDebug("Parsing clipboard data, length: {Length}", clipboardData.Length);

            var normalizedData = clipboardData.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = normalizedData.Split(new[] { '\n' }, StringSplitOptions.None);

            var lastNonEmptyLine = lines.Length - 1;
            while (lastNonEmptyLine >= 0 && string.IsNullOrEmpty(lines[lastNonEmptyLine]))
            {
                lastNonEmptyLine--;
            }

            if (lastNonEmptyLine < 0)
                return new string[0, 0];

            var actualLines = lines.Take(lastNonEmptyLine + 1).ToArray();

            if (actualLines.Length == 0)
                return new string[0, 0];

            var maxCols = actualLines.Max(line => line.Split('\t').Length);

            if (actualLines.Length == 1 && !actualLines[0].Contains('\t'))
            {
                var result = new string[1, 1];
                result[0, 0] = actualLines[0];
                _logger.LogDebug("Parsed single cell data");
                return result;
            }

            var resultArray = new string[actualLines.Length, maxCols];

            for (int i = 0; i < actualLines.Length; i++)
            {
                var cells = actualLines[i].Split('\t');
                for (int j = 0; j < maxCols; j++)
                {
                    resultArray[i, j] = j < cells.Length ? (cells[j] ?? "") : "";
                }
            }

            _logger.LogDebug("Parsed clipboard data to {Rows}x{Cols} array", actualLines.Length, maxCols);
            return resultArray;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing clipboard data from Excel format");
            var fallbackResult = new string[1, 1];
            fallbackResult[0, 0] = clipboardData ?? "";
            return fallbackResult;
        }
    }

    public async Task CopySelectedCellsAsync(string[,] selectedData)
    {
        try
        {
            var tabDelimited = ConvertToExcelFormat(selectedData);
            await SetClipboardDataAsync(tabDelimited);
            _logger.LogDebug("Copied selected cells to clipboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying selected cells");
        }
    }

    public async Task<string[,]> PasteStructuredDataAsync()
    {
        try
        {
            var clipboardText = await GetClipboardDataAsync();
            if (string.IsNullOrEmpty(clipboardText))
                return new string[0, 0];

            return ParseFromExcelFormat(clipboardText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pasting structured data");
            return new string[0, 0];
        }
    }

    private string ConvertToHtmlFormat(string tabDelimitedData)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("<table>");

            var lines = tabDelimitedData.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;

                sb.AppendLine("<tr>");
                var cells = line.Split('\t');
                foreach (var cell in cells)
                {
                    sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to HTML format");
            return tabDelimitedData;
        }
    }
}