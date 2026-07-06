using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WindowsPrinter.Models;

namespace WindowsPrinter.Converters;

public sealed class AppStatusSeverityToInfoBarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is AppStatusSeverity severity ? severity switch
        {
            AppStatusSeverity.Success => InfoBarSeverity.Success,
            AppStatusSeverity.Warning => InfoBarSeverity.Warning,
            AppStatusSeverity.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational
        } : InfoBarSeverity.Informational;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
