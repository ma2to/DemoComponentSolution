using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RpaWinUIComponents.AdvancedDataGrid.Models;

/// <summary>
/// Represents a single row in the DataGrid with validation support
/// </summary>
[ObservableObject]
public partial class GridDataRow
{
    [ObservableProperty]
    private bool hasValidationErrors;

    [ObservableProperty]
    private bool isEmpty = true;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string validationErrorsText = string.Empty;

    public ObservableCollection<CellViewModel> Cells { get; } = new();
    public Dictionary<string, object?> CellValues { get; } = new();

    public string RowId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the value of a specific column
    /// </summary>
    public T? GetValue<T>(string columnName)
    {
        if (CellValues.TryGetValue(columnName, out var value))
        {
            if (value == null)
                return default(T);

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }
        return default(T);
    }

    /// <summary>
    /// Sets the value of a specific column
    /// </summary>
    public void SetValue(string columnName, object? value)
    {
        CellValues[columnName] = value;

        // Find corresponding cell and update it
        var cell = Cells.FirstOrDefault(c => c.ColumnName == columnName);
        if (cell != null)
        {
            cell.Value = value;
        }

        UpdateEmptyStatus();
        OnPropertyChanged(nameof(CellValues));
    }

    /// <summary>
    /// Gets a cell by column name
    /// </summary>
    public CellViewModel? GetCell(string columnName)
    {
        return Cells.FirstOrDefault(c => c.ColumnName == columnName);
    }

    /// <summary>
    /// Adds a new cell to this row
    /// </summary>
    public void AddCell(CellViewModel cell)
    {
        Cells.Add(cell);
        CellValues[cell.ColumnName] = cell.Value;

        // Subscribe to cell value changes
        cell.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CellViewModel.Value))
            {
                CellValues[cell.ColumnName] = cell.Value;
                UpdateEmptyStatus();
                UpdateValidationStatus();
            }
        };

        UpdateEmptyStatus();
    }

    /// <summary>
    /// Updates the validation status based on all cells
    /// </summary>
    public void UpdateValidationStatus()
    {
        HasValidationErrors = Cells.Any(c => c.HasValidationError);

        var errors = Cells
            .Where(c => c.HasValidationError && !string.IsNullOrEmpty(c.ValidationErrorText))
            .Select(c => $"{c.ColumnName}: {c.ValidationErrorText}")
            .ToList();

        ValidationErrorsText = string.Join("; ", errors);
    }

    /// <summary>
    /// Updates empty status based on non-special columns
    /// </summary>
    private void UpdateEmptyStatus()
    {
        var dataCells = Cells.Where(c => !IsSpecialColumn(c.ColumnName));

        IsEmpty = dataCells.All(c =>
            c.Value == null ||
            string.IsNullOrWhiteSpace(c.Value?.ToString()));
    }

    private static bool IsSpecialColumn(string columnName)
    {
        return columnName == "DeleteAction" || columnName == "ValidAlerts";
    }
}