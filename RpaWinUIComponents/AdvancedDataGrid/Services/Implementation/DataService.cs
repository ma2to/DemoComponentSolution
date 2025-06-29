using Microsoft.Extensions.Logging;
using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RpaWinUIComponents.AdvancedDataGrid.Services.Implementation;

/// <summary>
/// Implementation of data service for managing grid data
/// </summary>
public class DataService : IDataService
{
    private readonly ILogger<DataService> _logger;
    private List<GridDataRow> _rows = new();
    private List<ColumnDefinition> _columns = new();

    public DataService(ILogger<DataService>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DataService>.Instance;
    }

    public event EventHandler<DataChangeEventArgs>? DataChanged;
    public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

    public void Initialize(List<ColumnDefinition> columns)
    {
        try
        {
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            _logger.LogInformation("DataService initialized with {ColumnCount} columns", _columns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing DataService");
            throw;
        }
    }

    public async Task LoadDataAsync(DataTable dataTable)
    {
        try
        {
            _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);

            _rows.Clear();

            if (dataTable != null)
            {
                await Task.Run(() =>
                {
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        var gridRow = new GridDataRow();

                        foreach (var column in _columns)
                        {
                            var cell = new CellViewModel
                            {
                                ColumnName = column.Name,
                                DataType = column.DataType
                            };

                            if (dataTable.Columns.Contains(column.Name))
                            {
                                var value = dataRow[column.Name];
                                cell.SetValueWithoutValidation(value == DBNull.Value ? null : value);
                            }

                            gridRow.AddCell(cell);
                        }

                        _rows.Add(gridRow);
                    }
                });
            }

            _logger.LogInformation("Successfully loaded {RowCount} rows from DataTable", _rows.Count);
            OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from DataTable");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
        }
    }

    public async Task LoadDataAsync(List<Dictionary<string, object>> data)
    {
        try
        {
            _logger.LogInformation("Loading data from dictionary list with {RowCount} rows", data?.Count ?? 0);

            _rows.Clear();

            if (data != null)
            {
                await Task.Run(() =>
                {
                    foreach (var dataRow in data)
                    {
                        var gridRow = new GridDataRow();

                        foreach (var column in _columns)
                        {
                            var cell = new CellViewModel
                            {
                                ColumnName = column.Name,
                                DataType = column.DataType
                            };

                            if (dataRow.ContainsKey(column.Name))
                            {
                                cell.SetValueWithoutValidation(dataRow[column.Name]);
                            }

                            gridRow.AddCell(cell);
                        }

                        _rows.Add(gridRow);
                    }
                });
            }

            _logger.LogInformation("Successfully loaded {RowCount} rows from dictionary list", _rows.Count);
            OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.LoadData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from dictionary list");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
        }
    }

    public async Task<DataTable> ExportDataAsync()
    {
        try
        {
            _logger.LogDebug("Exporting data to DataTable");

            return await Task.Run(() =>
            {
                var dataTable = new DataTable();

                // Add columns (excluding special columns)
                foreach (var column in _columns.Where(c => !c.IsSpecialColumn))
                {
                    var dataType = Nullable.GetUnderlyingType(column.DataType) ?? column.DataType;
                    dataTable.Columns.Add(column.Name, dataType);
                }

                // Add rows (excluding empty rows)
                foreach (var row in _rows.Where(r => !r.IsEmpty))
                {
                    var dataRow = dataTable.NewRow();
                    foreach (var column in _columns.Where(c => !c.IsSpecialColumn))
                    {
                        var value = row.GetValue<object>(column.Name);
                        dataRow[column.Name] = value ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }

                return dataTable;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to DataTable");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
            return new DataTable();
        }
    }

    public async Task ClearAllDataAsync()
    {
        try
        {
            _logger.LogInformation("Clearing all data");

            await Task.Run(() =>
            {
                foreach (var row in _rows)
                {
                    foreach (var cell in row.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.SetValidationError(false, string.Empty);
                    }
                }
            });

            _logger.LogInformation("Successfully cleared all data");
            OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.ClearData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all data");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
        }
    }

    public async Task<bool> ValidateAllRowsAsync()
    {
        try
        {
            _logger.LogDebug("Validating all rows");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating all rows");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
            return false;
        }
    }

    public async Task RemoveRowsByConditionAsync(string columnName, Func<object?, bool> condition)
    {
        try
        {
            _logger.LogDebug("Removing rows by condition for column: {ColumnName}", columnName);

            var rowsToRemove = await Task.Run(() =>
            {
                return _rows
                    .Where(row => !row.IsEmpty)
                    .Where(row =>
                    {
                        var value = row.GetValue<object>(columnName);
                        return condition(value);
                    })
                    .ToList();
            });

            foreach (var row in rowsToRemove)
            {
                _rows.Remove(row);
            }

            _logger.LogInformation("Removed {Count} rows by condition for column: {ColumnName}", rowsToRemove.Count, columnName);
            OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveRows });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing rows by condition for column: {ColumnName}", columnName);
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
        }
    }

    public async Task RemoveEmptyRowsAsync()
    {
        try
        {
            _logger.LogDebug("Removing empty rows");

            var result = await Task.Run(() =>
            {
                var dataRows = _rows.Where(r => !r.IsEmpty).ToList();
                var removedCount = _rows.Count - dataRows.Count;
                return new { DataRows = dataRows, RemovedCount = removedCount };
            });

            _rows = result.DataRows;

            _logger.LogInformation("Removed {Count} empty rows", result.RemovedCount);
            OnDataChanged(new DataChangeEventArgs { ChangeType = DataChangeType.RemoveEmptyRows });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing empty rows");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
        }
    }

    public async Task<List<GridDataRow>> CreateEmptyRowsAsync(int count)
    {
        try
        {
            return await Task.Run(() =>
            {
                var rows = new List<GridDataRow>();

                for (int i = 0; i < count; i++)
                {
                    var row = new GridDataRow();
                    foreach (var column in _columns)
                    {
                        var cell = new CellViewModel
                        {
                            ColumnName = column.Name,
                            DataType = column.DataType,
                            Value = null
                        };
                        row.AddCell(cell);
                    }
                    rows.Add(row);
                }

                return rows;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating empty rows");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "CreateEmptyRowsAsync"));
            return new List<GridDataRow>();
        }
    }

    private static bool IsSpecialColumn(string columnName)
    {
        return columnName == "DeleteAction" || columnName == "ValidAlerts";
    }

    protected virtual void OnDataChanged(DataChangeEventArgs e)
    {
        DataChanged?.Invoke(this, e);
    }

    protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
    {
        ErrorOccurred?.Invoke(this, e);
    }
}