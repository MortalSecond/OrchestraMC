using System;
using System.Globalization;
using System.Windows.Data;

namespace MinecraftServerTool.Helpers
{
    public class BoolToEnabledDisabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "Enabled" : "Disabled";
            return "Disabled"; // default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s == "Enabled";
            return false; // default
        }
    }
}
