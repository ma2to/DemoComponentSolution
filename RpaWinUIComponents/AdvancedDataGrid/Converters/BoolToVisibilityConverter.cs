using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace RpaWinUIComponents.AdvancedDataGrid.Converters;

/// <summary>
/// Converter for boolean to visibility conversion
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            if (Invert)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            var result = visibility == Visibility.Visible;
            return Invert ? !result : result;
        }

        return false;
    }
}