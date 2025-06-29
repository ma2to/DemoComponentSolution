using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace RpaWinUIComponents.AdvancedDataGrid.Converters;

/// <summary>
/// Converter for validation error visual states
/// </summary>
public class ValidationErrorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool hasErrors)
        {
            if (targetType == typeof(Brush))
            {
                if (parameter?.ToString() == "Background")
                {
                    return hasErrors
                        ? new SolidColorBrush(Color.FromArgb(30, 255, 0, 0))
                        : new SolidColorBrush(Colors.Transparent);
                }
                else
                {
                    return hasErrors
                        ? new SolidColorBrush(Colors.Red)
                        : new SolidColorBrush(Colors.Gray);
                }
            }
            else if (targetType == typeof(Thickness))
            {
                return hasErrors ? new Thickness(2) : new Thickness(1);
            }
            else if (targetType == typeof(Visibility))
            {
                return hasErrors ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}