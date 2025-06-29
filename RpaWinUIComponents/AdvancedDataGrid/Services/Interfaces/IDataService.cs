using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;

/// <summary>
/// Service for handling data operations
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Initializes the service with column definitions
    /// </summary>
    void Initialize(List<ColumnDefinition> columns);

    /// <summary>
    /// Loads data from DataTable
    /// </summary>
    Task LoadDataAsync(DataTable dataTable);

    /// <summary>
    /// Loads data from dictionary collection
    /// </summary>
    Task LoadDataAsync(List<Dictionary<string, object>> data);

    /// <summary>
    /// Exports data to DataTable
    /// </summary>
    Task<DataTable> ExportDataAsync();

    /// <summary>
    /// Clears all data
    /// </summary>
    Task ClearAllDataAsync();

    /// <summary>
    /// Validates all rows
    /// </summary>
    Task<bool> ValidateAllRowsAsync();

    /// <summary>
    /// Removes rows by condition
    /// </summary>
    Task RemoveRowsByConditionAsync(string columnName, Func<object?, bool> condition);

    /// <summary>
    /// Removes empty rows
    /// </summary>
    Task RemoveEmptyRowsAsync();

    /// <summary>
    /// Creates empty rows for the grid
    /// </summary>
    Task<List<GridDataRow>> CreateEmptyRowsAsync(int count);

    /// <summary>
    /// Event fired when data changes
    /// </summary>
    event EventHandler<DataChangeEventArgs>? DataChanged;

    /// <summary>
    /// Event fired when error occurs
    /// </summary>
    event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
}

/// <summary>
/// Arguments for data change events
/// </summary>
public class DataChangeEventArgs : EventArgs
{
    public DataChangeType ChangeType { get; set; }
    public object? ChangedData { get; set; }
    public string? ColumnName { get; set; }
    public int RowIndex { get; set; }
}

/// <summary>
/// Types of data changes
/// </summary>
public enum DataChangeType
{
    LoadData,
    ClearData,
    CellValueChanged,
    RemoveRows,
    RemoveEmptyRows,
    AddRow
}