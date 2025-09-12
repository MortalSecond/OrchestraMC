using MinecraftServerTool.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace MinecraftServerTool.Services
{
    public class ServerPropertiesService
    {
        public static Dictionary<string, string> ParseServerProperties(string modpackPath)
        {
            string filePath = Path.Combine(modpackPath, "server.properties");
            var dict = new Dictionary<string, string>();

            foreach (var line in File.ReadAllLines(filePath))
            {
                // Skips comments and empty lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    dict[key] = value;
                }
            }

            return dict;
        }

        public static void LoadServerProperties(Dictionary<string, string> props,ServerPropertiesViewModel vm)
        {
            vm.AllowFlight          = props.GetBoolOrNull("allow-flight");
            vm.AllowNether          = props.GetBoolOrNull("allow-nether");
            vm.CommandBlocks        = props.GetBoolOrNull("enable-command-block");
            vm.Difficulty           = props.GetOrNull("difficulty");
            vm.MaxPlayers           = props.GetIntOrNull("max-players");
            vm.LevelSeed            = props.GetOrNull("level-seed");
            vm.Hardcore             = props.GetBoolOrNull("hardcore");
            vm.Pvp                  = props.GetBoolOrNull("pvp");
            vm.MaxPlayers           = props.GetIntOrNull("max-players");
            vm.OnlineMode           = props.GetBoolOrNull("online-mode");
            vm.NetworkCompression   = props.GetOrNull("network-compression-treshold");
            vm.LevelName            = props.GetOrNull("level-name");
            vm.MaxWorldSize         = props.GetIntOrNull("max-world-size");
            vm.LevelSeed            = props.GetOrNull("level-seed");
        }

        public static void SaveServerProperties(string modpackPath, ServerPropertiesViewModel viewModel)
        {
            string filePath = Path.Combine(modpackPath, "server.properties");
            var properties = ParseServerProperties(modpackPath);

            // Updates dictionary with ViewModel values
            properties["allow-flight"] = viewModel.AllowFlight.ToString();
            properties["allow-nether"] = viewModel.AllowNether.ToString();
            properties["enable-command-block"] = viewModel.CommandBlocks.ToString();
            properties["difficulty"] = viewModel.Difficulty;
            properties["hardcore"] = viewModel.Hardcore.ToString();
            properties["pvp"] = viewModel.Pvp.ToString();
            properties["max-players"] = viewModel.MaxPlayers.ToString();
            properties["online-mode"] = viewModel.OnlineMode.ToString();
            properties["network-compression-treshold"] = viewModel.NetworkCompression.ToString();
            properties["level-name"] = viewModel.LevelName.ToString();
            properties["max-world-size"] = viewModel.MaxWorldSize.ToString();
            properties["level-seed"] = viewModel.LevelSeed.ToString();

            // Writes back to file
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var kvp in properties)
                {
                    writer.WriteLine($"{kvp.Key}={kvp.Value}");
                }
            }
        }

    }
    // This whole class is dedicated purely to ensure the values extracted have a value
    // or else returns a null. This makes it so the ServerPropertiesControl panel is
    // compatible with all Minecraft versions -- should the earlier versions not have
    // one of the fields requested and whatnot. Otherwise it would throw a KeyNotFoundException.
    public static class DictionaryExtensions
    {
        // Why isn't string? (with a question mark) allowed?
        public static string GetOrNull(this Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var value) ? value : null;
        }

        public static bool? GetBoolOrNull(this Dictionary<string, string> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            return null;
        }

        public static int? GetIntOrNull(this Dictionary<string, string> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && int.TryParse(value, out var number))
                return number;
            return null;
        }
    }

}
