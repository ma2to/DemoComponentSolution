using System;

namespace RpaWinUIComponents.AdvancedDataGrid.Events;

/// <summary>
/// Event arguments for component errors
/// </summary>
public class ComponentErrorEventArgs : EventArgs
{
    public Exception Exception { get; set; }
    public string Operation { get; set; }
    public string AdditionalInfo { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public ComponentErrorEventArgs(Exception exception, string operation, string? additionalInfo = null)
    {
        Exception = exception;
        Operation = operation;
        AdditionalInfo = additionalInfo ?? string.Empty;
    }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Operation}: {Exception.Message}" +
               (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");
    }
}