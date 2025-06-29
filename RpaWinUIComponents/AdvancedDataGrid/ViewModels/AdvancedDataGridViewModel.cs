using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUIComponents.AdvancedDataGrid.ViewModels;

/// <summary>
/// Main ViewModel for the AdvancedDataGrid component
/// </summary>
[ObservableObject]
public partial class AdvancedDataGridViewModel : IAsyncDisposable
{
    private readonly IValidationService _validationService;
    private readonly IClipboardService _clipboardService;
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<AdvancedDataGridViewModel> _logger;

    private bool _disposed = false;
    private readonly Dictionary<string, CancellationTokenSource> _pendingValidations = new();
    private readonly SemaphoreSlim _validationSemaphore;

    [ObservableProperty]
    private ObservableCollection<GridDataRow> rows = new();

    [ObservableProperty]
    private ObservableCollection<ColumnDefinition> columns = new();

    [ObservableProperty]
    private bool isValidating;

    [ObservableProperty]
    private double validationProgress;

    [ObservableProperty]
    private string validationStatus = "Pripravené";

    [ObservableProperty]
    private bool isInitialized;

    [ObservableProperty]
    private int initialRowCount = 100;

    [ObservableProperty]
    private ThrottlingConfig throttlingConfig = ThrottlingConfig.Default;

    [ObservableProperty]
    private bool isKeyboardShortcutsVisible;

    public AdvancedDataGridViewModel(
        IValidationService validationService,
        IClipboardService clipboardService,
        IDataService dataService,
        INavigationService navigationService,
        ILogger<AdvancedDataGridViewModel>? logger = null)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvancedDataGridViewModel>.Instance;

        _validationSemaphore = new SemaphoreSlim(ThrottlingConfig.MaxConcurrentValidations, ThrottlingConfig.MaxConcurrentValidations);

        SubscribeToEvents();
        _logger.LogDebug("AdvancedDataGridViewModel created");
    }

    #region Events

    public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task ValidateAllAsync()
    {
        await ValidateAllRowsAsync();
    }

    [RelayCommand]
    private async Task ClearAllDataAsync()
    {
        await ClearAllDataInternalAsync();
    }

    [RelayCommand]
    private async Task RemoveEmptyRowsAsync()
    {
        await RemoveEmptyRowsInternalAsync();
    }

    [RelayCommand]
    private async Task CopySelectedCellsAsync()
    {
        await CopyInternalAsync();
    }

    [RelayCommand]
    private async Task PasteFromClipboardAsync()
    {
        await PasteInternalAsync();
    }

    [RelayCommand]
    private void DeleteRow(GridDataRow? row)
    {
        if (row != null)
        {
            DeleteRowInternal(row);
        }
    }

    [RelayCommand]
    private async Task ExportToDataTableAsync()
    {
        await ExportDataInternalAsync();
    }

    [RelayCommand]
    private void ToggleKeyboardShortcuts()
    {
        IsKeyboardShortcutsVisible = !IsKeyboardShortcutsVisible;
        _logger.LogDebug("Keyboard shortcuts visibility toggled to: {IsVisible}", IsKeyboardShortcutsVisible);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the component with column definitions and validation rules
    /// </summary>
    public async Task InitializeAsync(
        List<ColumnDefinition> columnDefinitions,
        List<ValidationRule>? validationRules = null,
        ThrottlingConfig? throttling = null,
        int initialRowCount = 100)
    {
        ThrowIfDisposed();

        try
        {
            if (IsInitialized)
            {
                _logger.LogWarning("Component already initialized. Call Reset() first if needed.");
                return;
            }

            InitialRowCount = Math.Max(1, Math.Min(initialRowCount, 10000));
            ThrottlingConfig = throttling ?? ThrottlingConfig.Default;
            ThrottlingConfig.Validate();

            UpdateValidationSemaphore();

            _logger.LogInformation("Initializing AdvancedDataGrid with {ColumnCount} columns, {RuleCount} validation rules, {InitialRowCount} rows",
                columnDefinitions?.Count ?? 0, validationRules?.Count ?? 0, InitialRowCount);

            var processedColumns = ProcessColumnDefinitions(columnDefinitions ?? new List<ColumnDefinition>());
            _dataService.Initialize(processedColumns);

            Columns.Clear();
            foreach (var column in processedColumns)
            {
                Columns.Add(column);
            }

            if (validationRules != null)
            {
                foreach (var rule in validationRules)
                {
                    _validationService.AddValidationRule(rule);
                }
                _logger.LogDebug("Added {RuleCount} validation rules", validationRules.Count);
            }

            await CreateInitialRowsAsync();
            _navigationService.Initialize(Rows.ToList(), processedColumns);

            IsInitialized = true;
            _logger.LogInformation("AdvancedDataGrid initialization completed: {ActualRowCount} rows created",
                Rows.Count);
        }
        catch (Exception ex)
        {
            IsInitialized = false;
            _logger.LogError(ex, "Error during initialization");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
        }
    }

    /// <summary>
    /// Loads data from DataTable with automatic validation
    /// </summary>
    public async Task LoadDataAsync(DataTable dataTable)
    {
        ThrowIfDisposed();

        try
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Component must be initialized first!");

            _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);

            IsValidating = true;
            ValidationStatus = "Načítavajú sa dáta...";
            ValidationProgress = 0;

            Rows.Clear();

            var newRows = new List<GridDataRow>();
            var rowIndex = 0;
            var totalRows = dataTable?.Rows.Count ?? 0;

            if (dataTable != null)
            {
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var gridRow = CreateRowForLoading();

                    foreach (var column in Columns.Where(c => !c.IsSpecialColumn))
                    {
                        if (dataTable.Columns.Contains(column.Name))
                        {
                            var value = dataRow[column.Name];
                            gridRow.SetValue(column.Name, value == DBNull.Value ? null : value);
                        }
                    }

                    await ValidateRowAfterLoading(gridRow);
                    newRows.Add(gridRow);
                    rowIndex++;
                    ValidationProgress = (double)rowIndex / totalRows * 90;
                }
            }

            // Auto-expansion logic
            var minEmptyRows = Math.Min(10, InitialRowCount / 5);
            var finalRowCount = Math.Max(InitialRowCount, totalRows + minEmptyRows);

            while (newRows.Count < finalRowCount)
            {
                newRows.Add(CreateEmptyRowWithRealTimeValidation());
            }

            foreach (var row in newRows)
            {
                Rows.Add(row);
            }

            ValidationStatus = "Validácia dokončená";
            ValidationProgress = 100;

            var validRows = newRows.Count(r => !r.IsEmpty && !r.HasValidationErrors);
            var invalidRows = newRows.Count(r => !r.IsEmpty && r.HasValidationErrors);
            var emptyRows = newRows.Count - totalRows;

            _logger.LogInformation("Data loaded with auto-expansion: {TotalRows} total rows ({DataRows} data, {EmptyRows} empty), {ValidRows} valid, {InvalidRows} invalid",
                newRows.Count, totalRows, emptyRows, validRows, invalidRows);

            await Task.Delay(2000);
            IsValidating = false;
            ValidationStatus = "Pripravené";
        }
        catch (Exception ex)
        {
            IsValidating = false;
            ValidationStatus = "Chyba pri načítavaní";
            _logger.LogError(ex, "Error loading data from DataTable");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
        }
    }

    /// <summary>
    /// Loads data from dictionary collection
    /// </summary>
    public async Task LoadDataAsync(List<Dictionary<string, object>> data)
    {
        ThrowIfDisposed();

        try
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Component must be initialized first!");

            var dataTable = ConvertToDataTable(data);
            await LoadDataAsync(dataTable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data from dictionary list");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
        }
    }

    /// <summary>
    /// Exports data to DataTable
    /// </summary>
    public async Task<DataTable> ExportDataAsync(bool includeValidAlerts = false)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Exporting data to DataTable, includeValidAlerts: {IncludeValidAlerts}", includeValidAlerts);
            var result = await _dataService.ExportDataAsync();
            _logger.LogInformation("Exported {RowCount} rows to DataTable", result.Rows.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
            return new DataTable();
        }
    }

    /// <summary>
    /// Validates all rows and returns true if all are valid
    /// </summary>
    public async Task<bool> ValidateAllRowsAsync()
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Starting validation of all rows");
            IsValidating = true;
            ValidationProgress = 0;
            ValidationStatus = "Validujú sa riadky...";

            var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
            var results = await _validationService.ValidateAllRowsAsync(dataRows);

            var allValid = results.All(r => r.IsValid);
            ValidationStatus = allValid ? "Všetky riadky sú validné" : "Nájdené validačné chyby";

            _logger.LogInformation("Validation completed: all valid = {AllValid}", allValid);

            await Task.Delay(2000);
            ValidationStatus = "Pripravené";
            IsValidating = false;

            return allValid;
        }
        catch (Exception ex)
        {
            IsValidating = false;
            ValidationStatus = "Chyba pri validácii";
            _logger.LogError(ex, "Error validating all rows");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
            return false;
        }
    }

    #endregion

    #region Private Methods

    private void SubscribeToEvents()
    {
        _validationService.ValidationCompleted += OnValidationCompleted;
        _validationService.ValidationErrorOccurred += OnValidationServiceErrorOccurred;
        _dataService.DataChanged += OnDataChanged;
        _dataService.ErrorOccurred += OnDataServiceErrorOccurred;
        _navigationService.ErrorOccurred += OnNavigationServiceErrorOccurred;
    }

    private void UnsubscribeFromEvents()
    {
        try
        {
            if (_validationService != null)
            {
                _validationService.ValidationCompleted -= OnValidationCompleted;
                _validationService.ValidationErrorOccurred -= OnValidationServiceErrorOccurred;
            }

            if (_dataService != null)
            {
                _dataService.DataChanged -= OnDataChanged;
                _dataService.ErrorOccurred -= OnDataServiceErrorOccurred;
            }

            if (_navigationService != null)
            {
                _navigationService.ErrorOccurred -= OnNavigationServiceErrorOccurred;
            }

            _logger?.LogDebug("All service events unsubscribed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error unsubscribing from service events");
        }
    }

    private List<ColumnDefinition> ProcessColumnDefinitions(List<ColumnDefinition> columns)
    {
        var result = new List<ColumnDefinition>();
        var existingNames = new List<string>();

        foreach (var column in columns)
        {
            var uniqueName = GenerateUniqueColumnName(column.Name, existingNames);
            var processedColumn = new ColumnDefinition
            {
                Name = uniqueName,
                DataType = column.DataType,
                MinWidth = column.MinWidth,
                MaxWidth = column.MaxWidth,
                AllowResize = column.AllowResize,
                AllowSort = column.AllowSort,
                IsReadOnly = column.IsReadOnly,
                DisplayName = column.DisplayName
            };

            result.Add(processedColumn);
            existingNames.Add(uniqueName);
        }

        // Add special columns in correct order
        var deleteActionColumn = columns.FirstOrDefault(c => c.Name == "DeleteAction");
        if (deleteActionColumn == null)
        {
            result.Add(new ColumnDefinition("DeleteAction", typeof(object), 50, 50)
            {
                AllowResize = false,
                AllowSort = false,
                IsReadOnly = true,
                DisplayName = "Akcie"
            });
        }

        var validAlertsColumn = columns.FirstOrDefault(c => c.Name == "ValidAlerts");
        if (validAlertsColumn == null)
        {
            result.Add(new ColumnDefinition("ValidAlerts", typeof(string), 150, 400)
            {
                AllowResize = true,
                AllowSort = false,
                IsReadOnly = true,
                DisplayName = "Validačné chyby"
            });
        }

        return result;
    }

    private string GenerateUniqueColumnName(string baseName, List<string> existingNames)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Column";

        var uniqueName = baseName;
        var counter = 1;

        while (existingNames.Contains(uniqueName))
        {
            uniqueName = $"{baseName}_{counter}";
            counter++;
        }

        return uniqueName;
    }

    private async Task CreateInitialRowsAsync()
    {
        var rowCount = InitialRowCount;

        var rows = await Task.Run(() =>
        {
            var rowList = new List<GridDataRow>();

            for (int i = 0; i < rowCount; i++)
            {
                var row = CreateEmptyRowWithRealTimeValidation();
                rowList.Add(row);
            }

            return rowList;
        });

        Rows.Clear();
        foreach (var row in rows)
        {
            Rows.Add(row);
        }

        _logger.LogDebug("Created {RowCount} initial empty rows", rowCount);
    }

    private GridDataRow CreateRowForLoading()
    {
        var row = new GridDataRow();

        foreach (var column in Columns)
        {
            var cell = new CellViewModel
            {
                ColumnName = column.Name,
                DataType = column.DataType,
                Value = null
            };

            row.AddCell(cell);
        }

        return row;
    }

    private GridDataRow CreateEmptyRowWithRealTimeValidation()
    {
        var row = new GridDataRow();

        foreach (var column in Columns)
        {
            var cell = new CellViewModel
            {
                ColumnName = column.Name,
                DataType = column.DataType,
                Value = null
            };

            // Subscribe to real-time validation
            cell.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(CellViewModel.Value))
                {
                    await OnCellValueChangedRealTime(row, cell);
                }
            };

            row.AddCell(cell);
        }

        return row;
    }

    private async Task ValidateRowAfterLoading(GridDataRow row)
    {
        try
        {
            if (!row.IsEmpty)
            {
                foreach (var cell in row.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                {
                    await _validationService.ValidateCellAsync(cell, row);
                }

                row.UpdateValidationStatus();
            }

            foreach (var cell in row.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
            {
                cell.PropertyChanged += async (s, e) =>
                {
                    if (e.PropertyName == nameof(CellViewModel.Value))
                    {
                        await OnCellValueChangedRealTime(row, cell);
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating row after loading");
        }
    }

    private async Task OnCellValueChangedRealTime(GridDataRow row, CellViewModel cell)
    {
        if (_disposed) return;

        try
        {
            if (!ThrottlingConfig.IsEnabled)
            {
                await ValidateCellImmediately(row, cell);
                return;
            }

            var cellKey = $"{Rows.IndexOf(row)}_{cell.ColumnName}";

            if (_pendingValidations.TryGetValue(cellKey, out var existingCts))
            {
                existingCts.Cancel();
                _pendingValidations.Remove(cellKey);
            }

            if (row.IsEmpty)
            {
                cell.SetValidationError(false, string.Empty);
                row.UpdateValidationStatus();
                return;
            }

            var cts = new CancellationTokenSource();
            _pendingValidations[cellKey] = cts;

            try
            {
                await Task.Delay(ThrottlingConfig.TypingDelayMs, cts.Token);

                if (cts.Token.IsCancellationRequested || _disposed)
                    return;

                await ValidateCellThrottled(row, cell, cellKey, cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogTrace("Validation cancelled for cell: {CellKey}", cellKey);
            }
            finally
            {
                _pendingValidations.Remove(cellKey);
                cts.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in throttled cell validation");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCellValueChangedRealTime"));
        }
    }

    private async Task ValidateCellImmediately(GridDataRow row, CellViewModel cell)
    {
        try
        {
            if (row.IsEmpty)
            {
                cell.SetValidationError(false, string.Empty);
                row.UpdateValidationStatus();
                return;
            }

            await _validationService.ValidateCellAsync(cell, row);
            row.UpdateValidationStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in immediate cell validation");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellImmediately"));
        }
    }

    private async Task ValidateCellThrottled(GridDataRow row, CellViewModel cell, string cellKey, CancellationToken cancellationToken)
    {
        try
        {
            await _validationSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (cancellationToken.IsCancellationRequested || _disposed)
                    return;

                _logger.LogTrace("Executing throttled validation for cell: {CellKey}", cellKey);

                await _validationService.ValidateCellAsync(cell, row);
                row.UpdateValidationStatus();

                _logger.LogTrace("Throttled validation completed for cell: {CellKey}", cellKey);
            }
            finally
            {
                _validationSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("Throttled validation cancelled for cell: {CellKey}", cellKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in throttled validation for cell: {CellKey}", cellKey);
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellThrottled"));
        }
    }

    private async Task ClearAllDataInternalAsync()
    {
        ThrowIfDisposed();

        try
        {
            if (!IsInitialized) return;

            _logger.LogDebug("Clearing all data");

            await Task.Run(() =>
            {
                foreach (var row in Rows)
                {
                    foreach (var cell in row.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.SetValidationError(false, string.Empty);
                    }
                }
            });

            _logger.LogInformation("All data cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all data");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataInternalAsync"));
        }
    }

    private async Task RemoveEmptyRowsInternalAsync()
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Removing empty rows");

            var result = await Task.Run(() =>
            {
                var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                var minEmptyRows = Math.Min(10, InitialRowCount / 5);
                var emptyRowsNeeded = Math.Max(minEmptyRows, InitialRowCount - dataRows.Count);

                var newEmptyRows = new List<GridDataRow>();
                for (int i = 0; i < emptyRowsNeeded; i++)
                {
                    newEmptyRows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                return new { DataRows = dataRows, EmptyRows = newEmptyRows };
            });

            Rows.Clear();
            foreach (var row in result.DataRows)
            {
                Rows.Add(row);
            }
            foreach (var row in result.EmptyRows)
            {
                Rows.Add(row);
            }

            _logger.LogInformation("Empty rows removed, {DataRowCount} data rows kept, {EmptyRowCount} empty rows added",
                result.DataRows.Count, result.EmptyRows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing empty rows");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsInternalAsync"));
        }
    }

    private async Task CopyInternalAsync()
    {
        ThrowIfDisposed();

        try
        {
            var currentCell = _navigationService.CurrentCell;
            if (currentCell != null)
            {
                var data = currentCell.Value?.ToString() ?? "";
                await _clipboardService.SetClipboardDataAsync(data);
                _logger.LogDebug("Copied cell data to clipboard");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying selected cells");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "CopyInternalAsync"));
        }
    }

    private async Task PasteInternalAsync()
    {
        ThrowIfDisposed();

        try
        {
            if (!IsInitialized) return;

            var clipboardData = await _clipboardService.GetClipboardDataAsync();
            if (string.IsNullOrEmpty(clipboardData)) return;

            var parsedData = _clipboardService.ParseFromExcelFormat(clipboardData);
            var startRowIndex = _navigationService.CurrentRowIndex;
            var startColumnIndex = _navigationService.CurrentColumnIndex;

            if (startRowIndex >= 0 && startColumnIndex >= 0)
            {
                var editableColumns = Columns.Where(c => !c.IsSpecialColumn).ToList();
                if (startColumnIndex < editableColumns.Count)
                {
                    await PasteDataAtPositionAsync(parsedData, startRowIndex, startColumnIndex);

                    if (ThrottlingConfig.IsEnabled && ThrottlingConfig.PasteDelayMs > 0)
                    {
                        await Task.Delay(ThrottlingConfig.PasteDelayMs);
                    }

                    _logger.LogDebug("Pasted data from clipboard at position [{Row},{Col}]",
                        startRowIndex, startColumnIndex);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pasting from clipboard");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "PasteInternalAsync"));
        }
    }

    private async Task PasteDataAtPositionAsync(string[,] data, int startRowIndex, int startColumnIndex)
    {
        try
        {
            var prepResult = await Task.Run(() =>
            {
                int dataRows = data.GetLength(0);
                int dataCols = data.GetLength(1);
                var editableColumns = Columns.Where(c => !c.IsSpecialColumn).ToList();

                var newRowsNeeded = Math.Max(0, (startRowIndex + dataRows) - Rows.Count);
                var additionalRows = new List<GridDataRow>();

                for (int i = 0; i < newRowsNeeded; i++)
                {
                    additionalRows.Add(CreateEmptyRowWithRealTimeValidation());
                }

                return new
                {
                    DataRows = dataRows,
                    DataCols = dataCols,
                    EditableColumns = editableColumns,
                    AdditionalRows = additionalRows
                };
            });

            // Add any needed rows
            foreach (var row in prepResult.AdditionalRows)
            {
                Rows.Add(row);
            }

            // Set the data
            for (int i = 0; i < prepResult.DataRows; i++)
            {
                int targetRowIndex = startRowIndex + i;
                if (targetRowIndex >= Rows.Count) break;

                for (int j = 0; j < prepResult.DataCols; j++)
                {
                    int targetColumnIndex = startColumnIndex + j;
                    if (targetColumnIndex >= prepResult.EditableColumns.Count) break;

                    var columnName = prepResult.EditableColumns[targetColumnIndex].Name;
                    var targetRow = Rows[targetRowIndex];

                    if (!IsSpecialColumn(columnName))
                    {
                        targetRow.SetValue(columnName, data[i, j]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pasting data at position");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "PasteDataAtPositionAsync"));
        }
    }

    private void DeleteRowInternal(GridDataRow row)
    {
        if (_disposed) return;

        try
        {
            if (Rows.Contains(row))
            {
                foreach (var cell in row.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                {
                    cell.Value = null;
                    cell.SetValidationError(false, string.Empty);
                }

                _logger.LogDebug("Row deleted");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting row");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "DeleteRowInternal"));
        }
    }

    private async Task<DataTable> ExportDataInternalAsync()
    {
        try
        {
            return await _dataService.ExportDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data internally");
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataInternalAsync"));
            return new DataTable();
        }
    }

    private DataTable ConvertToDataTable(List<Dictionary<string, object>> data)
    {
        var dataTable = new DataTable();

        if (data?.Count > 0)
        {
            foreach (var key in data[0].Keys)
            {
                dataTable.Columns.Add(key, typeof(object));
            }

            foreach (var row in data)
            {
                var dataRow = dataTable.NewRow();
                foreach (var kvp in row)
                {
                    dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
                }
                dataTable.Rows.Add(dataRow);
            }
        }

        return dataTable;
    }

    private void UpdateValidationSemaphore()
    {
        _validationSemaphore?.Dispose();
        var maxConcurrent = ThrottlingConfig.MaxConcurrentValidations;
        _validationSemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    private static bool IsSpecialColumn(string columnName)
    {
        return columnName == "DeleteAction" || columnName == "ValidAlerts";
    }

    #endregion

    #region Event Handlers

    private void OnValidationCompleted(object? sender, ValidationCompletedEventArgs e)
    {
        if (_disposed) return;
        _logger.LogTrace("Validation completed for row. Is valid: {IsValid}", e.IsValid);
    }

    private void OnValidationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
    {
        if (_disposed) return;
        _logger.LogError(e.Exception, "ValidationService error: {Operation}", e.Operation);
        OnErrorOccurred(e);
    }

    private void OnDataChanged(object? sender, DataChangeEventArgs e)
    {
        if (_disposed) return;
        _logger.LogTrace("Data changed: {ChangeType}", e.ChangeType);
    }

    private void OnDataServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
    {
        if (_disposed) return;
        _logger.LogError(e.Exception, "DataService error: {Operation}", e.Operation);
        OnErrorOccurred(e);
    }

    private void OnNavigationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
    {
        if (_disposed) return;
        _logger.LogError(e.Exception, "NavigationService error: {Operation}", e.Operation);
        OnErrorOccurred(e);
    }

    protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
    {
        if (_disposed) return;
        ErrorOccurred?.Invoke(this, e);
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                _logger?.LogDebug("Disposing AdvancedDataGridViewModel...");

                UnsubscribeFromEvents();

                // Cancel all pending validations
                foreach (var cts in _pendingValidations.Values)
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                _pendingValidations.Clear();

                // Clear collections
                Rows?.Clear();
                Columns?.Clear();

                // Dispose semaphore
                _validationSemaphore?.Dispose();

                _disposed = true;
                _logger?.LogInformation("AdvancedDataGridViewModel disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during AdvancedDataGridViewModel disposal");
            }
        }
    }

    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AdvancedDataGridViewModel));
    }

    #endregion
}