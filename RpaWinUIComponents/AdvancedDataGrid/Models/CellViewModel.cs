using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace RpaWinUIComponents.AdvancedDataGrid.Models;

/// <summary>
/// ViewModel for a single cell in the DataGrid
/// </summary>
[ObservableObject]
public partial class CellViewModel
{
    [ObservableProperty]
    private object? value;

    [ObservableProperty]
    private object? originalValue;

    [ObservableProperty]
    private bool hasValidationError;

    [ObservableProperty]
    private string validationErrorText = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private bool hasUnsavedChanges;

    public string ColumnName { get; set; } = string.Empty;
    public Type DataType { get; set; } = typeof(string);
    public string CellId { get; } = Guid.NewGuid().ToString();

    partial void OnValueChanged(object? value)
    {
        if (OriginalValue == null && !IsEditing)
        {
            OriginalValue = value;
        }

        UpdateUnsavedChangesStatus();
    }

    partial void OnIsEditingChanged(bool value)
    {
        if (value)
        {
            StartEditing();
        }
        else
        {
            EndEditing();
        }
    }

    /// <summary>
    /// Starts editing mode
    /// </summary>
    public void StartEditing()
    {
        OriginalValue = Value;
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Ends editing mode and commits changes
    /// </summary>
    public void EndEditing()
    {
        OriginalValue = Value;
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Cancels editing and reverts to original value
    /// </summary>
    public void CancelEditing()
    {
        if (HasUnsavedChanges && OriginalValue != Value)
        {
            Value = OriginalValue;

            if (HasValidationError)
            {
                SetValidationError(false, string.Empty);
            }
        }

        HasUnsavedChanges = false;
        IsEditing = false;
    }

    /// <summary>
    /// Commits changes
    /// </summary>
    public void CommitChanges()
    {
        if (HasUnsavedChanges)
        {
            EndEditing();
        }
    }

    /// <summary>
    /// Sets validation error state
    /// </summary>
    public void SetValidationError(bool hasError, string errorText)
    {
        HasValidationError = hasError;
        ValidationErrorText = errorText;
    }

    /// <summary>
    /// Sets value without triggering validation
    /// </summary>
    public void SetValueWithoutValidation(object? newValue)
    {
        var oldValue = Value;
        Value = newValue;
        OriginalValue = newValue;
        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Gets typed value with safe conversion
    /// </summary>
    public T? GetTypedValue<T>()
    {
        try
        {
            if (Value == null)
                return default(T);

            if (Value is T directValue)
                return directValue;

            return (T)Convert.ChangeType(Value, typeof(T));
        }
        catch
        {
            return default(T);
        }
    }

    private void UpdateUnsavedChangesStatus()
    {
        HasUnsavedChanges = IsEditing && !AreValuesEqual(Value, OriginalValue);
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;

        if (value1 is string str1 && value2 is string str2)
        {
            return string.Equals(str1?.Trim(), str2?.Trim());
        }

        return value1.Equals(value2);
    }
}