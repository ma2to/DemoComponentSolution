using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RpaWinUIComponents.AdvancedDataGrid.Helpers;
using RpaWinUIComponents.AdvancedDataGrid.Models;
using System;

namespace RpaWinUIComponents.AdvancedDataGrid.Behaviors;

/// <summary>
/// Behavior for handling real-time validation visual feedback
/// </summary>
public class ValidationBehavior : BehaviorBase<FrameworkElement>
{
    private readonly ILogger<ValidationBehavior> _logger;

    public ValidationBehavior()
    {
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ValidationBehavior>.Instance;
    }

    #region Dependency Properties

    public static readonly DependencyProperty CellViewModelProperty =
        DependencyProperty.Register(
            nameof(CellViewModel),
            typeof(CellViewModel),
            typeof(ValidationBehavior),
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
        UpdateValidationState();
    }

    private static void OnCellViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ValidationBehavior behavior)
        {
            if (e.OldValue is CellViewModel oldCell)
            {
                oldCell.PropertyChanged -= behavior.OnCellPropertyChanged;
            }

            if (e.NewValue is CellViewModel newCell)
            {
                newCell.PropertyChanged += behavior.OnCellPropertyChanged;
                behavior.UpdateValidationState();
            }
        }
    }

    private void OnCellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CellViewModel.HasValidationError) ||
            e.PropertyName == nameof(CellViewModel.ValidationErrorText))
        {
            UpdateValidationState();
        }
    }

    private void UpdateValidationState()
    {
        try
        {
            if (AssociatedObject == null || CellViewModel == null) return;

            // Update attached properties for visual state changes
            AttachedProperties.SetHasValidationError(AssociatedObject, CellViewModel.HasValidationError);
            AttachedProperties.SetValidationErrorText(AssociatedObject, CellViewModel.ValidationErrorText);

            _logger.LogTrace("Updated validation state for cell {ColumnName}: HasError={HasError}",
                CellViewModel.ColumnName, CellViewModel.HasValidationError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating validation state");
        }
    }

    protected override void OnAssociatedObjectUnloaded()
    {
        if (CellViewModel != null)
        {
            CellViewModel.PropertyChanged -= OnCellPropertyChanged;
        }
        base.OnAssociatedObjectUnloaded();
    }
}