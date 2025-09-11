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

        public static void SaveProperties(string filePath,Dictionary<string, string> props,ServerPropertiesViewModel vm)
        {
            // Updates keys
            props["allow-flight"] = vm.AllowFlight.ToString().ToLower();
            props["allow-nether"] = vm.AllowNether.ToString().ToLower();
            props["enable-command-block"] = vm.CommandBlocks.ToString().ToLower();
            props["difficulty"] = vm.Difficulty;
            props["hardcore"] = vm.Hardcore.ToString().ToLower();
            props["pvp"] = vm.Pvp.ToString().ToLower();
            props["max-players"] = vm.MaxPlayers.ToString();
            props["online-mode"] = vm.OnlineMode.ToString().ToLower();
            props["network-compression-threshold"] = vm.NetworkCompression;
            props["level-name"] = vm.LevelName;
            props["max-world-size"] = vm.MaxWorldSize.ToString();
            props["level-seed"] = vm.LevelSeed ?? "";

            // Rewrites
            var writer = new StreamWriter(filePath);
            foreach (var kv in props)
            {
                writer.WriteLine($"{kv.Key}={kv.Value}");
            }
        }

    }
}
