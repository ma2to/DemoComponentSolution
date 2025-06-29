using System;

namespace RpaWinUIComponents.AdvancedDataGrid.Models;

/// <summary>
/// Represents a validation rule for a specific column
/// </summary>
public class ValidationRule
{
    public string ColumnName { get; set; } = string.Empty;
    public Func<object?, GridDataRow, bool> ValidationFunction { get; set; } = (value, row) => true;
    public string ErrorMessage { get; set; } = string.Empty;
    public Func<GridDataRow, bool> ApplyCondition { get; set; } = _ => true;
    public int Priority { get; set; } = 0;
    public string RuleName { get; set; }

    public ValidationRule()
    {
        RuleName = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Checks if this rule should be applied to the given row
    /// </summary>
    public bool ShouldApply(GridDataRow row)
    {
        try
        {
            return ApplyCondition?.Invoke(row) ?? true;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Validates the value according to this rule
    /// </summary>
    public bool Validate(object? value, GridDataRow row)
    {
        try
        {
            if (!ShouldApply(row))
                return true;

            return ValidationFunction?.Invoke(value, row) ?? true;
        }
        catch
        {
            return false;
        }
    }
}