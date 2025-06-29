using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RpaWinUIComponents.AdvancedDataGrid.Events;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using RpaWinUIComponents.AdvancedDataGrid.Services.Implementation;
using RpaWinUIComponents.AdvancedDataGrid.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace RpaWinUIComponents.AdvancedDataGrid.Controls;

/// <summary>
/// Advanced DataGrid control with real-time validation, copy/paste, and Excel-like functionality
/// </summary>
[TemplatePart(Name = PART_ItemsView, Type = typeof(ItemsView))]
[TemplatePart(Name = PART_ValidationProgress, Type = typeof(ProgressBar))]
[TemplatePart(Name = PART_KeyboardShortcuts, Type = typeof(Border))]
public sealed class AdvancedDataGridControl : Control
{
    private const string PART_ItemsView = "PART_ItemsView";
    private const string PART_ValidationProgress = "PART_ValidationProgress";
    private const string PART_KeyboardShortcuts = "PART_KeyboardShortcuts";

    private ItemsView? _itemsView;
    private ProgressBar? _validationProgress;
    private Border? _keyboardShortcuts;
    private AdvancedDataGridViewModel? _viewModel;
    private bool _isLoaded = false;

    #region Dependency Properties

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(object),
            typeof(AdvancedDataGridControl),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty IsValidatingProperty =
        DependencyProperty.Register(
            nameof(IsValidating),
            typeof(bool),
            typeof(AdvancedDataGridControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ValidationProgressProperty =
        DependencyProperty.Register(
            nameof(ValidationProgress),
            typeof(double),
            typeof(AdvancedDataGridControl),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty ValidationStatusProperty =
        DependencyProperty.Register(
            nameof(ValidationStatus),
            typeof(string),
            typeof(AdvancedDataGridControl),
            new PropertyMetadata("Pripravené"));

    public static readonly DependencyProperty IsKeyboardShortcutsVisibleProperty =
        DependencyProperty.Register(
            nameof(IsKeyboardShortcutsVisible),
            typeof(bool),
            typeof(AdvancedDataGridControl),
            new PropertyMetadata(false, OnIsKeyboardShortcutsVisibleChanged));

    #endregion

    #region Properties

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public bool IsValidating
    {
        get => (bool)GetValue(IsValidatingProperty);
        set => SetValue(IsValidatingProperty, value);
    }

    public double ValidationProgress
    {
        get => (double)GetValue(ValidationProgressProperty);
        set => SetValue(ValidationProgressProperty, value);
    }

    public string ValidationStatus
    {
        get => (string)GetValue(ValidationStatusProperty);
        set => SetValue(ValidationStatusProperty, value);
    }

    public bool IsKeyboardShortcutsVisible
    {
        get => (bool)GetValue(IsKeyboardShortcutsVisibleProperty);
        set => SetValue(IsKeyboardShortcutsVisibleProperty, value);
    }

    public bool IsInitialized => _viewModel?.IsInitialized ?? false;

    #endregion

    #region Events

    public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

    #endregion

    public AdvancedDataGridControl()
    {
        this.DefaultStyleKey = typeof(AdvancedDataGridControl);
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;

        SetupKeyboardAccelerators();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsView = GetTemplateChild(PART_ItemsView) as ItemsView;
        _validationProgress = GetTemplateChild(PART_ValidationProgress) as ProgressBar;
        _keyboardShortcuts = GetTemplateChild(PART_KeyboardShortcuts) as Border;

        if (_isLoaded)
        {
            InitializeViewModel();
        }
    }

    #region Public API Methods

    /// <summary>
    /// Initializes the component with column definitions and validation rules
    /// </summary>
    public async Task InitializeAsync(
        List<ColumnDefinition> columns,
        List<ValidationRule>? validationRules = null,
        ThrottlingConfig? throttling = null,
        int initialRowCount = 100)
    {
        try
        {
            if (_viewModel == null)
            {
                InitializeViewModel();
            }

            if (_viewModel != null)
            {
                await _viewModel.InitializeAsync(columns, validationRules, throttling, initialRowCount);
                UpdateBindings();
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
        }
    }

    /// <summary>
    /// Loads data from DataTable
    /// </summary>
    public async Task LoadDataAsync(DataTable dataTable)
    {
        try
        {
            if (_viewModel == null)
                throw new InvalidOperationException("Component must be initialized first! Call InitializeAsync().");

            await _viewModel.LoadDataAsync(dataTable);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
        }
    }

    /// <summary>
    /// Loads data from dictionary collection
    /// </summary>
    public async Task LoadDataAsync(List<Dictionary<string, object>> data)
    {
        try
        {
            if (_viewModel == null)
                throw new InvalidOperationException("Component must be initialized first! Call InitializeAsync().");

            await _viewModel.LoadDataAsync(data);
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
        }
    }

    /// <summary>
    /// Exports data to DataTable
    /// </summary>
    public async Task<DataTable> ExportDataAsync()
    {
        try
        {
            if (_viewModel == null)
                return new DataTable();

            return await _viewModel.ExportDataAsync();
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
            return new DataTable();
        }
    }

    /// <summary>
    /// Validates all rows
    /// </summary>
    public async Task<bool> ValidateAllAsync()
    {
        try
        {
            if (_viewModel == null)
                return false;

            return await _viewModel.ValidateAllRowsAsync();
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllAsync"));
            return false;
        }
    }

    /// <summary>
    /// Clears all data
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        try
        {
            if (_viewModel?.ClearAllDataCommand?.CanExecute(null) == true)
            {
                await _viewModel.ClearAllDataCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
        }
    }

    /// <summary>
    /// Removes empty rows
    /// </summary>
    public async Task RemoveEmptyRowsAsync()
    {
        try
        {
            if (_viewModel?.RemoveEmptyRowsCommand?.CanExecute(null) == true)
            {
                await _viewModel.RemoveEmptyRowsCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
        }
    }

    /// <summary>
    /// Removes rows by condition
    /// </summary>
    public async Task RemoveRowsByConditionAsync(string columnName, Func<object?, bool> condition)
    {
        try
        {
            if (_viewModel == null) return;

            var rowsToRemove = _viewModel.Rows
                .Where(row => !row.IsEmpty)
                .Where(row =>
                {
                    var value = row.GetValue<object>(columnName);
                    return condition(value);
                })
                .ToList();

            foreach (var row in rowsToRemove)
            {
                _viewModel.Rows.Remove(row);
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
        }
    }

    /// <summary>
    /// Removes rows by custom validation rules
    /// </summary>
    public async Task<int> RemoveRowsByValidationAsync(List<ValidationRule> customValidationRules)
    {
        try
        {
            if (_viewModel == null || customValidationRules?.Count == 0)
                return 0;

            var rowsToRemove = new List<GridDataRow>();
            var dataRows = _viewModel.Rows.Where(r => !r.IsEmpty).ToList();

            foreach (var row in dataRows)
            {
                foreach (var rule in customValidationRules)
                {
                    var cell = row.GetCell(rule.ColumnName);
                    if (cell != null && !rule.Validate(cell.Value, row))
                    {
                        rowsToRemove.Add(row);
                        break;
                    }
                }
            }

            foreach (var row in rowsToRemove)
            {
                _viewModel.Rows.Remove(row);
            }

            return rowsToRemove.Count;
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByValidationAsync"));
            return 0;
        }
    }

    #endregion

    #region Private Methods

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        if (_itemsView != null)
        {
            InitializeViewModel();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        CleanupViewModel();
    }

    private void InitializeViewModel()
    {
        if (_viewModel != null) return;

        try
        {
            var validationService = new ValidationService();
            var clipboardService = new ClipboardService();
            var dataService = new DataService();
            var navigationService = new NavigationService();

            _viewModel = new AdvancedDataGridViewModel(
                validationService,
                clipboardService,
                dataService,
                navigationService);

            _viewModel.ErrorOccurred += OnViewModelErrorOccurred;

            this.DataContext = _viewModel;
            UpdateBindings();
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeViewModel"));
        }
    }

    private void CleanupViewModel()
    {
        if (_viewModel != null)
        {
            _viewModel.ErrorOccurred -= OnViewModelErrorOccurred;
            _ = Task.Run(async () => await _viewModel.DisposeAsync());
            _viewModel = null;
        }
    }

    private void UpdateBindings()
    {
        if (_viewModel == null) return;

        // Bind ItemsSource
        if (_itemsView != null)
        {
            _itemsView.ItemsSource = _viewModel.Rows;
        }

        // Update dependency properties
        IsValidating = _viewModel.IsValidating;
        ValidationProgress = _viewModel.ValidationProgress;
        ValidationStatus = _viewModel.ValidationStatus;
        IsKeyboardShortcutsVisible = _viewModel.IsKeyboardShortcutsVisible;

        // Subscribe to property changes