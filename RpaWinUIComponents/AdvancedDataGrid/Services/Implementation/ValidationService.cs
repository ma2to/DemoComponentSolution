using Microsoft.Extensions.Logging;
using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Implementation;

/// <summary>
/// Implementation of validation service
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly Dictionary<string, List<ValidationRule>> _validationRules = new();

    public ValidationService(ILogger<ValidationService>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ValidationService>.Instance;
    }

    public event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;
    public event EventHandler<ComponentErrorEventArgs>? ValidationErrorOccurred;

    public async Task<ValidationResult> ValidateCellAsync(CellViewModel cell, GridDataRow row)
    {
        try
        {
            return await Task.Run(() =>
            {
                var result = new ValidationResult(true)
                {
                    ColumnName = cell.ColumnName,
                    CellId = cell.CellId
                };

                if (row.IsEmpty)
                {
                    cell.SetValidationError(false, string.Empty);
                    _logger.LogTrace("Validation skipped for empty row, column: {ColumnName}", cell.ColumnName);
                    return result;
                }

                if (!_validationRules.ContainsKey(cell.ColumnName))
                    return result;

                var rules = _validationRules[cell.ColumnName]
                    .Where(r => r.ShouldApply(row))
                    .OrderByDescending(r => r.Priority);

                var errorMessages = new List<string>();

                foreach (var rule in rules)
                {
                    try
                    {
                        if (!rule.Validate(cell.Value, row))
                        {
                            errorMessages.Add(rule.ErrorMessage);
                            result.IsValid = false;
                            _logger.LogDebug("Validation rule '{RuleName}' failed for {ColumnName} = '{Value}'",
                                rule.RuleName, cell.ColumnName, cell.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Validation rule '{RuleName}' threw exception for {ColumnName}",
                            rule.RuleName, cell.ColumnName);
                        errorMessages.Add($"Chyba validácie: {ex.Message}");
                        result.IsValid = false;
                    }
                }

                result.ErrorMessages = errorMessages;
                cell.SetValidationError(!result.IsValid, string.Join("; ", errorMessages));

                if (errorMessages.Count > 0)
                {
                    _logger.LogDebug("Validation failed for {ColumnName} = '{Value}': {Errors}",
                        cell.ColumnName, cell.Value, string.Join(", ", errorMessages));
                }

                return result;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cell {ColumnName}", cell?.ColumnName);
            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellAsync"));
            return ValidationResult.Failure($"Chyba pri validácii: {ex.Message}");
        }
    }

    public async Task<List<ValidationResult>> ValidateRowAsync(GridDataRow row)
    {
        try
        {
            var results = new List<ValidationResult>();

            if (row.IsEmpty)
            {
                foreach (var cell in row.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                {
                    cell.SetValidationError(false, string.Empty);
                }

                row.UpdateValidationStatus();
                _logger.LogTrace("Row validation skipped for empty row");

                OnValidationCompleted(new ValidationCompletedEventArgs
                {
                    Row = row,
                    Results = results
                });

                return results;
            }

            _logger.LogDebug("Validating row with {CellCount} cells", row.Cells.Count);

            var cellsToValidate = row.Cells
                .Where(c => !IsSpecialColumn(c.ColumnName) && _validationRules.ContainsKey(c.ColumnName))
                .ToList();

            foreach (var cell in cellsToValidate)
            {
                var result = await ValidateCellAsync(cell, row);
                results.Add(result);
            }

            row.UpdateValidationStatus();

            var validCount = results.Count(r => r.IsValid);
            var invalidCount = results.Count(r => !r.IsValid);
            _logger.LogDebug("Row validation completed: {ValidCount} valid, {InvalidCount} invalid", validCount, invalidCount);

            OnValidationCompleted(new ValidationCompletedEventArgs
            {
                Row = row,
                Results = results
            });

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating row");
            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateRowAsync"));
            return new List<ValidationResult>();
        }
    }

    public async Task<List<ValidationResult>> ValidateAllRowsAsync(IEnumerable<GridDataRow> rows)
    {
        try
        {
            var allResults = new List<ValidationResult>();
            var dataRows = rows.Where(r => !r.IsEmpty).ToList();

            if (dataRows.Count == 0)
            {
                _logger.LogInformation("ValidateAllRows: No non-empty rows to validate");
                return allResults;
            }

            _logger.LogInformation("Validating {RowCount} non-empty rows", dataRows.Count);

            const int batchSize = 10;
            var totalRows = dataRows.Count;
            var processedRows = 0;

            for (int i = 0; i < dataRows.Count; i += batchSize)
            {
                var batch = dataRows.Skip(i).Take(batchSize).ToList();

                var batchTasks = batch.Select(async row =>
                {
                    try
                    {
                        return await ValidateRowAsync(row);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error validating row in batch");
                        return new List<ValidationResult>();
                    }
                });

                var batchResults = await Task.WhenAll(batchTasks);

                foreach (var rowResults in batchResults)
                {
                    allResults.AddRange(rowResults);
                }

                processedRows += batch.Count;
                _logger.LogDebug("Validated batch: {ProcessedRows}/{TotalRows} rows", processedRows, totalRows);
            }

            var validCount = allResults.Count(r => r.IsValid);
            var invalidCount = allResults.Count(r => !r.IsValid);
            _logger.LogInformation("ValidateAllRows completed: {ValidCount} valid, {InvalidCount} invalid results", validCount, invalidCount);

            return allResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating all rows");
            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
            return new List<ValidationResult>();
        }
    }

    public void AddValidationRule(ValidationRule rule)
    {
        try
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            if (string.IsNullOrWhiteSpace(rule.ColumnName))
                throw new ArgumentException("ColumnName cannot be null or empty", nameof(rule));

            if (!_validationRules.ContainsKey(rule.ColumnName))
            {
                _validationRules[rule.ColumnName] = new List<ValidationRule>();
            }

            _validationRules[rule.ColumnName].RemoveAll(r => r.RuleName == rule.RuleName);
            _validationRules[rule.ColumnName].Add(rule);

            _logger.LogDebug("Added validation rule '{RuleName}' for column '{ColumnName}'", rule.RuleName, rule.ColumnName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding validation rule");
            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "AddValidationRule"));
        }
    }

    public void RemoveValidationRule(string columnName, string ruleName)
    {
        try
        {
            if (_validationRules.ContainsKey(columnName))
            {
                var removedCount = _validationRules[columnName].RemoveAll(r => r.RuleName == ruleName);
                _logger.LogDebug("Removed {RemovedCount} validation rule(s) '{RuleName}' from column '{ColumnName}'",
                    removedCount, ruleName, columnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing validation rule '{RuleName}' from column '{ColumnName}'", ruleName, columnName);
            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveValidationRule"));
        }
    }

    public void ClearValidationRules(string? columnName = null)
    {
        try
        {
            if (columnName == null)
            {
                var totalRules = _validationRules.Values.Sum(rules => rules.Count);
                _validationRules.Clear();
                _logger.LogInformation("Cleared all {TotalRules} validation rules", totalRules);
            }
            else if (_validationRules.ContainsKey(columnName))
            {
                var ruleCount = _validationRules[columnName].Count;
                _validationRules[columnName].Clear();
                _logger.LogDebug("Cleared {RuleCount} validation rules from column '{ColumnName}'", ruleCount, columnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing validation rules for column: {ColumnName}", columnName ?? "ALL");
            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "ClearValidationRules"));
        }
    }

    public List<ValidationRule> GetValidationRules(string columnName)
    {
        try
        {
            return _validationRules.TryGetValue(columnName, out var rules)
                ? new List<ValidationRule>(rules)
                : new List<ValidationRule>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation rules for column: {ColumnName}", columnName);
            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, "GetValidationRules"));
            return new List<ValidationRule>();
        }
    }

    public bool HasValidationRules(string columnName)
    {
        return _validationRules.ContainsKey(columnName) && _validationRules[columnName].Count > 0;
    }

    public int GetTotalRuleCount()
    {
        return _validationRules.Values.Sum(rules => rules.Count);
    }

    private static bool IsSpecialColumn(string columnName)
    {
        return columnName == "DeleteAction" || columnName == "ValidAlerts";
    }

    protected virtual void OnValidationCompleted(ValidationCompletedEventArgs e)
    {
        ValidationCompleted?.Invoke(this, e);
    }

    protected virtual void OnValidationErrorOccurred(ComponentErrorEventArgs e)
    {
        ValidationErrorOccurred?.Invoke(this, e);
    }
}