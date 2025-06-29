using RpaWinUIComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpaWinUIComponents.AdvancedDataGrid.Events;

/// <summary>
/// Event arguments for validation completion
/// </summary>
public class ValidationCompletedEventArgs : EventArgs
{
    public GridDataRow? Row { get; set; }
    public CellViewModel? Cell { get; set; }
    public List<ValidationResult> Results { get; set; } = new();
    public bool IsValid => Results.All(r => r.IsValid);
    public string ErrorSummary => string.Join("; ", Results.Where(r => !r.IsValid).Select(r => r.ErrorText));
}