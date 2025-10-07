namespace NijhofPanel.Views.Converters;

using System.Windows.Controls;
using System.Windows.Data;

public class TreeViewLineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var item = (TreeViewItem)value;
        var ic = ItemsControl.ItemsControlFromItemContainer(item);
        return ic.ItemContainerGenerator.IndexFromContainer(item) == ic.Items.Count - 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return false;
    }
}