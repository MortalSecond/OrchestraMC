using System;
using System.Globalization;
using System.Windows.Data;

namespace MinecraftServerTool.Helpers
{
    public class BoolToEnabledDisabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // I've had to research like six different type parsing methods because value
            // always starts as object type and literally none of them look the same.
            // value.ToString(), (int)value, value is bool, etc.
            // So sorry if the code looks messy, but conversions are a bitch and we live in hell.
            if (value is bool b)
            {
                if (b)
                    return "Enabled";
                else
                    return "Disabled";
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.ToString() == "Enabled")
                return true;
            else
                return false;
        }
    }
    public class DifficultyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This is kinda hacky, but for some reason i can't
            // use ToTitleCase(). Hardcoding each case seems like
            // the next best thing in its place.
            switch (value?.ToString())
            {
                case "peaceful":
                    return "Peaceful";
                case "easy":
                    return "Easy";
                case "normal":
                    return "Normal";
                case "hard":
                    return "Hard";
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().ToLower();
        }
    }
    public class CompressionConverter : IValueConverter
    {
        public object Convert(object value, Type type, object parameter, CultureInfo culture)
        {
            // Yes, the switch does need the 64, 128, 256, and 512 to turn
            // it into itself. Even though it looks redundant, do NOT remove
            // those fields or else the combobox will get bricked.
            // It just looks like it's turning a string into a string because
            // the value has already been ToString()'d, but in reality it's
            // turning an object type into a string type.
            // Goddamn pain in the ass.
            switch (value?.ToString())
            {
                case "0":
                    return "Everything";
                case "64":
                    return "64";
                case "128":
                    return "128";
                case "256":
                    return "256";
                case "512":
                    return "512";
                case "-1":
                    return "Disabled";
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
        {
            // I have no idea why the code demands a null value unless there's
            // a question mark besides value, but don't remove it at any cost or
            // else the whole program crashes and throws an exception.
            // I think it throws a Can't Convert Null To Int error because all comboboxes
            // are blank at startup, but since my dumbass made the server properties panel
            // be housed in the same window as MainWindow, the code tries to retrieve the
            // values on startup. Essentially it's always getting a null/blank value.
            // So basically i fucked myself up by populating the forms on Window_Load.
            switch (value?.ToString())
            {
                case "Everything":
                    return "0";
                case "Disabled":
                    return "-1";
                default:
                    return null;
            }
        }
    }
    public class SeedConverter : IValueConverter
    {
        public object Convert(object value, Type type, object parameter, CultureInfo culture)
        {
            var text = value?.ToString();
            if (string.IsNullOrEmpty(text))
                return "Randomized";
            else
                return text;
        }
        public object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
        {
            var text = value?.ToString();
            if (text == "Randomized")
                return "";
            else
                return text;
        }
    }
}