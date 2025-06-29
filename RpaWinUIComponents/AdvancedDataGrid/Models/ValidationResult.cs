using System.Collections.Generic;

namespace RpaWinUIComponents.AdvancedDataGrid.Models;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public string ColumnName { get; set; } = string.Empty;
    public int RowIndex { get; set; }
    public string CellId { get; set; } = string.Empty;

    public ValidationResult(bool isValid = true)
    {
        IsValid = isValid;
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result with single error message
    /// </summary>
    public static ValidationResult Failure(string errorMessage)
    {
        return new ValidationResult(false)
        {
            ErrorMessages = new List<string> { errorMessage ?? string.Empty }
        };
    }

    /// <summary>
    /// Creates a failed validation result with multiple error messages
    /// </summary>
    public static ValidationResult Failure(List<string> errorMessages)
    {
        return new ValidationResult(false)
        {
            ErrorMessages = errorMessages ?? new List<string>()
        };
    }

    /// <summary>
    /// Gets combined error message text
    /// </summary>
    public string ErrorText => string.Join("; ", ErrorMessages);

    /// <summary>
    /// Adds an error message to the result
    /// </summary>
    public void AddError(string errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            ErrorMessages.Add(errorMessage);
            IsValid = false;
        }
    }

    /// <summary>
    /// Combines multiple validation results
    /// </summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var combined = new ValidationResult(true);

        foreach (var result in results)
        {
            if (!result.IsValid)
            {
                combined.IsValid = false;
                combined.ErrorMessages.AddRange(result.ErrorMessages);
            }
        }

        return combined;
    }
}