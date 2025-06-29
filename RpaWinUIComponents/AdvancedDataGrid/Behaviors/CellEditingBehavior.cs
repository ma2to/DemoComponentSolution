using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RpaWinUIComponents.AdvancedDataGrid.Helpers;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using System;

namespace RpaWinUIComponents.AdvancedDataGrid.Behaviors;

/// <summary>
/// Behavior for handling cell editing interactions
/// </summary>
public class CellEditingBehavior : BehaviorBase<FrameworkElement>
{
    private readonly ILogger<CellEditingBehavior> _logger;

    public CellEditingBehavior()
    {
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CellEditingBehavior>.Instance;
    }

    #region Dependency Properties

    public static readonly DependencyProperty CellViewModelProperty =
        DependencyProperty.Register(
            nameof(CellViewModel),
            typeof(CellViewModel),
            typeof(CellEditingBehavior),
            new PropertyMetadata(null, OnCellViewModelChanged));

    public CellViewModel? CellViewModel
    {
        get => (CellViewModel?)GetValue(CellViewModelProperty);
        set => SetValue(CellViewModelProperty, value);
    }

    #endregion

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        if (AssociatedObject != null)
        {
            AssociatedObject.DoubleTapped += OnDoubleTapped;
            AssociatedObject.GotFocus += OnGotFocus;
            AssociatedObject.LostFocus += OnLostFocus;

            // Subscribe to TextBox specific events if applicable
            if (AssociatedObject is TextBox textBox)
            {
                textBox.TextChanged += OnTextChanged;
            }

            UpdateEditingState();
        }
    }

    protected override void OnAssociatedObjectUnloaded()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.DoubleTapped -= OnDoubleTapped;
            AssociatedObject.GotFocus -= OnGotFocus;
            AssociatedObject.LostFocus -= OnLostFocus;

            if (AssociatedObject is TextBox textBox)
            {
                textBox.TextChanged -= OnTextChanged;
            }
        }

        if (CellViewModel != null)
        {
            CellViewModel.PropertyChanged -= OnCellPropertyChanged;
        }

        base.OnAssociatedObjectUnloaded();
    }

    private static void OnCellViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CellEditingBehavior behavior)
        {
            if (e.OldValue is CellViewModel oldCell)
            {
                oldCell.PropertyChanged -= behavior.OnCellPropertyChanged;
            }

            if (e.NewValue is CellViewModel newCell)
            {
                newCell.PropertyChanged += behavior.OnCellPropertyChanged;
                behavior.UpdateEditingState();
            }
        }
    }

    private void OnCellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CellViewModel.IsEditing) ||
            e.PropertyName == nameof(CellViewModel.IsSelected))
        {
            UpdateEditingState();
        }
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        try
        {
            if (CellViewModel != null && !IsSpecialColumn(CellViewModel.ColumnName))
            {
                CellViewModel.IsEditing = true;
                _logger.LogDebug("Double-tap started editing for {ColumnName}", CellViewModel.ColumnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling double-tap");
        }
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CellViewModel != null)
            {
                CellViewModel.IsSelected = true;
                AttachedProperties.SetIsSelected(AssociatedObject!, true);
                _logger.LogTrace("Cell {ColumnName} got focus", CellViewModel.ColumnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling got focus");
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CellViewModel != null && CellViewModel.IsEditing)
            {
                CellViewModel.CommitChanges();
                CellViewModel.IsEditing = false;
                _logger.LogDebug("Cell {ColumnName} lost focus, committed changes", CellViewModel.ColumnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling lost focus");
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (CellViewModel != null && sender is TextBox textBox)
            {
                CellViewModel.Value = textBox.Text;
                _logger.LogTrace("Text changed for {ColumnName}: '{Value}'", CellViewModel.ColumnName, textBox.Text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling text changed");
        }
    }

    private void UpdateEditingState()
    {
        try
        {
            if (AssociatedObject == null || CellViewModel == null) return;

            // Update attached properties for visual state changes
            AttachedProperties.SetIsEditing(AssociatedObject, CellViewModel.IsEditing);
            AttachedProperties.SetIsSelected(AssociatedObject, CellViewModel.IsSelected);
            AttachedProperties.SetCellId(AssociatedObject, CellViewModel.CellId);

            _logger.LogTrace("Updated editing state for cell {ColumnName}: IsEditing={IsEditing}, IsSelected={IsSelected}",
                CellViewModel.ColumnName, CellViewModel.IsEditing, CellViewModel.IsSelected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating editing state");
        }
    }

    private static bool IsSpecialColumn(string columnName)
    {
        return columnName == "DeleteAction" || columnName == "ValidAlerts";
    }
}