namespace MinecraftServerTool.Resources
{
    public static class Tooltips
    {
        public const string AllowFlight = "Determines whether players moving at really high speeds is allowed, or if it causes the player to be kicked from the server. Note that some mods require this to be on due to fast movement, like Create: Protection Pixel's jetpacks.";

        public const string Difficulty = "Peaceful disables mobs and hunger, while health regens to all hearts. \nEasy makes mobs deal less damage, and you can't die from hunger or poison. \nNormal makes mobs deal their regular damage, and hunger depletes the health bar to half a heart. \nHard makes mobs deal extra damage and hunger can kill.";

        public const string CommandBlocks = "When disabled, command blocks can be spawned but not interacted with, not even by admins or in creative. They need to be enabled for them to actually be usable.";

        public const string EnableStatus = "Determines whether the server appears as offline. If disabled, the server looks like it's offline, but it will allow players to join in.";

        public const string Hardcore = "Turns hardcore mode on. In Hardcore servers, players who die will be sent to spectator mode and be unable to respawn.";

        public const string LevelName = "The name of the world. By default it's called 'World'";

        public const string LevelSeed = "The seed for the worldgen. By default it is random.";

        public const string MaxWorldSize = "The amount of blocks from the center of the world to the world border.";

        public const string MOTD = "The message displayed in the server list. It supports color and formatting codes.";

        public const string NetworkCompressionTreshold = "How big should a packet be compressed, default being 256 kilobytes. Somewhat CPU-intensive. \nLower values can make players with bad internet connections have less lag, but will strain your CPU and give the host lag instead. \nHigher values make it easier on the host, but might make it harder for low-end players to connect to the server.";

        public const string OnlineMode = "When enabled, the server verifies players with Mojang’s authentication servers. Disabling it will allow players with cracked Minecraft to join your server.";

        public const string PlayerIdleTimeout = "How many minutes a player has to idle before getting kicked from the server.";

        public const string Pvp = "If enabled, players can damage each other.";

        public const string SimulationDistance = "Max distance, in chunks, that entities (mobs, items, blocks) can be from players to be updated. Lower number means less lag, but can mess with automation and farms.";

        public const string SpawnMonsters = "Whether monsters can spawn. If the server is set to peaceful, then monsters will not spawn even if enabled.";

        public const string SpawnProtection = "Defines the radius around the world spawn where only operators can modify blocks. Prevents griefing at the spawn point.";

        public const string ViewDistance = "Determines server-wide viewing distance. If the host has a slow connection, it might cause lag to have high values.";

    }
}