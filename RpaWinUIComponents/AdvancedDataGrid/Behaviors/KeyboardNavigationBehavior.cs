using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RpaWinUIComponents.AdvancedDataGrid.Services.Interfaces;
using System;
using Windows.System;

namespace RpaWinUIComponents.AdvancedDataGrid.Behaviors;

/// <summary>
/// Behavior for handling keyboard navigation in the DataGrid
/// </summary>
public class KeyboardNavigationBehavior : BehaviorBase<ItemsView>
{
    private readonly ILogger<KeyboardNavigationBehavior> _logger;

    public KeyboardNavigationBehavior()
    {
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<KeyboardNavigationBehavior>.Instance;
    }

    #region Dependency Properties

    public static readonly DependencyProperty NavigationServiceProperty =
        DependencyProperty.Register(
            nameof(NavigationService),
            typeof(INavigationService),
            typeof(KeyboardNavigationBehavior),
            new PropertyMetadata(null));

    public INavigationService? NavigationService
    {
        get => (INavigationService?)GetValue(NavigationServiceProperty);
        set => SetValue(NavigationServiceProperty, value);
    }

    #endregion

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        if (AssociatedObject != null)
        {
            AssociatedObject.KeyDown += OnKeyDown;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
            _logger.LogDebug("KeyboardNavigationBehavior attached to ItemsView");
        }
    }

    protected override void OnAssociatedObjectUnloaded()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.KeyDown -= OnKeyDown;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
            _logger.LogDebug("KeyboardNavigationBehavior detached from ItemsView");
        }

        base.OnAssociatedObjectUnloaded();
    }

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            // Handle key combinations that need to be processed before default handling
            if (NavigationService == null) return;

            var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var isShiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            switch (e.Key)
            {
                case VirtualKey.Tab:
                    HandleTabKey(isShiftPressed);
                    e.Handled = true;
                    break;

                case VirtualKey.Enter:
                    if (!isShiftPressed)
                    {
                        HandleEnterKey();
                        e.Handled = true;
                    }
                    break;

                case VirtualKey.Escape:
                    HandleEscapeKey();
                    e.Handled = true;
                    break;

                case VirtualKey.F2:
                    HandleF2Key();
                    e.Handled = true;
                    break;

                case VirtualKey.Delete:
                    HandleDeleteKey();
                    e.Handled = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PreviewKeyDown handler");
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            // Handle regular key navigation
            if (NavigationService == null) return;

            switch (e.Key)
            {
                case VirtualKey.Up:
                    NavigationService.MoveToPreviousRow();
                    e.Handled = true;
                    break;

                case VirtualKey.Down:
                    NavigationService.MoveToNextRow();
                    e.Handled = true;
                    break;

                case VirtualKey.Left:
                    NavigationService.MoveToPreviousCell();
                    e.Handled = true;
                    break;

                case VirtualKey.Right:
                    NavigationService.MoveToNextCell();
                    e.Handled = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in KeyDown handler");
        }
    }

    private void HandleTabKey(bool isShiftPressed)
    {
        try
        {
            if (NavigationService == null) return;

            if (isShiftPressed)
                NavigationService.MoveToPreviousCell();
            else
                NavigationService.MoveToNextCell();

            _logger.LogDebug("TAB navigation executed: {Direction}", isShiftPressed ? "Previous" : "Next");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Tab key");
        }
    }

    private void HandleEnterKey()
    {
        try
        {
            if (NavigationService == null) return;

            NavigationService.MoveToNextRow();
            _logger.LogDebug("ENTER navigation executed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Enter key");
        }
    }

    private void HandleEscapeKey()
    {
        try
        {
            var currentCell = NavigationService?.CurrentCell;
            if (currentCell != null)
            {
                currentCell.CancelEditing();
                _logger.LogDebug("ESC - cancelled editing for {ColumnName}", currentCell.ColumnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Escape key");
        }
    }

    private void HandleF2Key()
    {
        try
        {
            var currentCell = NavigationService?.CurrentCell;
            if (currentCell != null && !currentCell.IsEditing)
            {
                currentCell.IsEditing = true;
                _logger.LogDebug("F2 - started editing {ColumnName}", currentCell.ColumnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling F2 key");
        }
    }

    private void HandleDeleteKey()
    {
        try
        {
            var currentCell = NavigationService?.CurrentCell;
            if (currentCell != null && !IsSpecialColumn(currentCell.ColumnName))
            {
                currentCell.StartEditing();
                currentCell.Value = null;
                currentCell.CommitChanges();
                _logger.LogDebug("DELETE - cleared value in {ColumnName}", currentCell.ColumnName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Delete key");
        }
    }

    private static bool IsSpecialColumn(string columnName)
    {
        return columnName == "DeleteAction" || columnName == "ValidAlerts";
    }
}