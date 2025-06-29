using RpaWinUIComponents.AdvancedDataGrid.Models;
using System;

namespace RpaWinUIComponents.AdvancedDataGrid.Helpers;

/// <summary>
/// Static helper methods for creating validation rules
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Creates a required field validation rule
    /// </summary>
    public static ValidationRule Required(string columnName, string? errorMessage = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
            ErrorMessage = errorMessage ?? $"{columnName} je povinné pole",
            RuleName = $"{columnName}_Required"
        };
    }

    /// <summary>
    /// Creates a string length validation rule
    /// </summary>
    public static ValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = (value, row) =>
            {
                var text = value?.ToString() ?? "";
                return text.Length >= minLength && text.Length <= maxLength;
            },
            ErrorMessage = errorMessage ?? $"{columnName} musí mať dĺžku medzi {minLength} a {maxLength} znakmi",
            RuleName = $"{columnName}_Length"
        };
    }

    /// <summary>
    /// Creates a numeric range validation rule
    /// </summary>
    public static ValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = (value, row) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return true;

                if (double.TryParse(value.ToString(), out double numValue))
                {
                    return numValue >= min && numValue <= max;
                }

                return false;
            },
            ErrorMessage = errorMessage ?? $"{columnName} musí byť medzi {min} a {max}",
            RuleName = $"{columnName}_Range"
        };
    }

    /// <summary>
    /// Creates a conditional validation rule
    /// </summary>
    public static ValidationRule Conditional(
        string columnName,
        Func<object?, GridDataRow, bool> validationFunction,
        Func<GridDataRow, bool> condition,
        string errorMessage,
        string? ruleName = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = validationFunction,
            ApplyCondition = condition,
            ErrorMessage = errorMessage,
            RuleName = ruleName ?? $"{columnName}_Conditional_{Guid.NewGuid().ToString("N")[..8]}"
        };
    }

    /// <summary>
    /// Creates a numeric validation rule
    /// </summary>
    public static ValidationRule Numeric(string columnName, string? errorMessage = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = (value, row) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return true;

                return double.TryParse(value.ToString(), out _);
            },
            ErrorMessage = errorMessage ?? $"{columnName} musí byť číslo",
            RuleName = $"{columnName}_Numeric"
        };
    }

    /// <summary>
    /// Creates an email validation rule
    /// </summary>
    public static ValidationRule Email(string columnName, string? errorMessage = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = (value, row) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return true;

                var email = value.ToString()!;
                return email.Contains("@") && email.Contains(".") && email.Length > 5;
            },
            ErrorMessage = errorMessage ?? $"{columnName} musí mať platný formát emailu",
            RuleName = $"{columnName}_Email"
        };
    }

    /// <summary>
    /// Creates a regular expression validation rule
    /// </summary>
    public static ValidationRule Regex(string columnName, string pattern, string? errorMessage = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = (value, row) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return true;

                return System.Text.RegularExpressions.Regex.IsMatch(value.ToString()!, pattern);
            },
            ErrorMessage = errorMessage ?? $"{columnName} nemá správny formát",
            RuleName = $"{columnName}_Regex"
        };
    }

    /// <summary>
    /// Creates a custom validation rule
    /// </summary>
    public static ValidationRule Custom(
        string columnName,
        Func<object?, GridDataRow, bool> validationFunction,
        string errorMessage,
        string? ruleName = null)
    {
        return new ValidationRule
        {
            ColumnName = columnName,
            ValidationFunction = validationFunction,
            ErrorMessage = errorMessage,
            RuleName = ruleName ?? $"{columnName}_Custom_{Guid.NewGuid().ToString("N")[..8]}"
        };
    }
}