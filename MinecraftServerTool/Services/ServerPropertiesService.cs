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
            if (props.TryGetValue("allow-flight", out var allowFlight))
                vm.AllowFlight = allowFlight.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (props.TryGetValue("allow-nether", out var allowNether))
                vm.AllowNether = allowNether.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (props.TryGetValue("enable-command-block", out var cmd))
                vm.CommandBlocks = cmd.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (props.TryGetValue("difficulty", out var diff))
                vm.Difficulty = diff;

            if (props.TryGetValue("hardcore", out var hardcore))
                vm.Hardcore = hardcore.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (props.TryGetValue("pvp", out var pvp))
                vm.Pvp = pvp.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (props.TryGetValue("max-players", out var maxPlayers) &&
                int.TryParse(maxPlayers, out var mp))
                vm.MaxPlayers = mp;

            if (props.TryGetValue("online-mode", out var onlineMode))
                vm.OnlineMode = onlineMode.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (props.TryGetValue("network-compression-threshold", out var nct))
                vm.NetworkCompression = nct;

            if (props.TryGetValue("level-name", out var levelName))
                vm.LevelName = levelName;

            if (props.TryGetValue("max-world-size", out var mws) &&
                int.TryParse(mws, out var mwsInt))
                vm.MaxWorldSize = mwsInt;

            if (props.TryGetValue("level-seed", out var seed))
                vm.LevelSeed = seed;
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
}
