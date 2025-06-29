using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;

/// <summary>
/// Service for handling validation operations
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a single cell
    /// </summary>
    Task<ValidationResult> ValidateCellAsync(CellViewModel cell, GridDataRow row);

    /// <summary>
    /// Validates an entire row
    /// </summary>
    Task<List<ValidationResult>> ValidateRowAsync(GridDataRow row);

    /// <summary>
    /// Validates all rows in the collection
    /// </summary>
    Task<List<ValidationResult>> ValidateAllRowsAsync(IEnumerable<GridDataRow> rows);

    /// <summary>
    /// Adds a validation rule
    /// </summary>
    void AddValidationRule(ValidationRule rule);

    /// <summary>
    /// Removes a validation rule
    /// </summary>
    void RemoveValidationRule(string columnName, string ruleName);

    /// <summary>
    /// Clears validation rules for a column or all columns
    /// </summary>
    void ClearValidationRules(string? columnName = null);

    /// <summary>
    /// Gets validation rules for a specific column
    /// </summary>
    List<ValidationRule> GetValidationRules(string columnName);

    /// <summary>
    /// Checks if column has validation rules
    /// </summary>
    bool HasValidationRules(string columnName);

    /// <summary>
    /// Gets total number of validation rules
    /// </summary>
    int GetTotalRuleCount();

    /// <summary>
    /// Event fired when validation is completed
    /// </summary>
    event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;

    /// <summary>
    /// Event fired when validation error occurs
    /// </summary>
    event EventHandler<ComponentErrorEventArgs>? ValidationErrorOccurred;
}