using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace RpaWinUIComponents.AdvancedDataGrid.Models;

/// <summary>
/// Defines a column configuration for the AdvancedDataGrid
/// </summary>
[ObservableObject]
public partial class ColumnDefinition
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private Type dataType = typeof(string);

    [ObservableProperty]
    private double minWidth = 80;

    [ObservableProperty]
    private double maxWidth = 300;

    [ObservableProperty]
    private bool allowResize = true;

    [ObservableProperty]
    private bool allowSort = true;

    [ObservableProperty]
    private bool isReadOnly = false;

    [ObservableProperty]
    private string displayName = string.Empty;

    /// <summary>
    /// Indicates if this is a special system column (DeleteAction, ValidAlerts)
    /// </summary>
    public bool IsSpecialColumn => Name == "DeleteAction" || Name == "ValidAlerts";

    public ColumnDefinition()
    {
    }

    public ColumnDefinition(string name, Type dataType)
    {
        Name = name;
        DataType = dataType;
        DisplayName = name;
    }

    public ColumnDefinition(string name, Type dataType, double minWidth, double maxWidth)
        : this(name, dataType)
    {
        MinWidth = minWidth;
        MaxWidth = maxWidth;
    }
}