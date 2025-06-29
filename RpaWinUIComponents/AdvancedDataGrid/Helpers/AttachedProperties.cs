using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RpaWinUIComponents.AdvancedDataGrid.Helpers;

/// <summary>
/// Attached properties for validation and UI behavior
/// </summary>
public static class AttachedProperties
{
    #region HasValidationError

    public static readonly DependencyProperty HasValidationErrorProperty =
        DependencyProperty.RegisterAttached(
            "HasValidationError",
            typeof(bool),
            typeof(AttachedProperties),
            new PropertyMetadata(false, OnHasValidationErrorChanged));

    public static bool GetHasValidationError(DependencyObject obj)
    {
        return (bool)obj.GetValue(HasValidationErrorProperty);
    }

    public static void SetHasValidationError(DependencyObject obj, bool value)
    {
        obj.SetValue(HasValidationErrorProperty, value);
    }

    private static void OnHasValidationErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            var hasError = (bool)e.NewValue;
            VisualStateManager.GoToState(element, hasError ? "HasValidationError" : "Normal", true);
        }
    }

    #endregion

    #region ValidationErrorText

    public static readonly DependencyProperty ValidationErrorTextProperty =
        DependencyProperty.RegisterAttached(
            "ValidationErrorText",
            typeof(string),
            typeof(AttachedProperties),
            new PropertyMetadata(string.Empty));

    public static string GetValidationErrorText(DependencyObject obj)
    {
        return (string)obj.GetValue(ValidationErrorTextProperty);
    }

    public static void SetValidationErrorText(DependencyObject obj, string value)
    {
        obj.SetValue(ValidationErrorTextProperty, value);
    }

    #endregion

    #region IsEditing

    public static readonly DependencyProperty IsEditingProperty =
        DependencyProperty.RegisterAttached(
            "IsEditing",
            typeof(bool),
            typeof(AttachedProperties),
            new PropertyMetadata(false, OnIsEditingChanged));

    public static bool GetIsEditing(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEditingProperty);
    }

    public static void SetIsEditing(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEditingProperty, value);
    }

    private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            var isEditing = (bool)e.NewValue;
            VisualStateManager.GoToState(element, isEditing ? "Editing" : "NotEditing", true);
        }
    }

    #endregion

    #region IsSelected

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.RegisterAttached(
            "IsSelected",
            typeof(bool),
            typeof(AttachedProperties),
            new PropertyMetadata(false, OnIsSelectedChanged));

    public static bool GetIsSelected(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsSelectedProperty);
    }

    public static void SetIsSelected(DependencyObject obj, bool value)
    {
        obj.SetValue(IsSelectedProperty, value);
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            var isSelected = (bool)e.NewValue;
            VisualStateManager.GoToState(element, isSelected ? "Selected" : "Unselected", true);
        }
    }

    #endregion

    #region CellId

    public static readonly DependencyProperty CellIdProperty =
        DependencyProperty.RegisterAttached(
            "CellId",
            typeof(string),
            typeof(AttachedProperties),
            new PropertyMetadata(string.Empty));

    public static string GetCellId(DependencyObject obj)
    {
        return (string)obj.GetValue(CellIdProperty);
    }

    public static void SetCellId(DependencyObject obj, string value)
    {
        obj.SetValue(CellIdProperty, value);
    }

    #endregion
}