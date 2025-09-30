using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MinecraftServerTool.Services
{
    public class UtilsService
    {
        private readonly UIService _uiService;
        private readonly MainWindow _mainWindow;
        private readonly HttpClient _httpClient;
        // Initializing class for the Maven Metadata JSON
        // JSON Structure:
        //  "1.1": [
        //      "1.1-1.3.2.1",
        //      "1.1-1.3.2.2",
        //      "1.1-1.3.2.3",
        //      "1.1-1.3.2.4",
        public class ForgeMetadata : Dictionary<string, List<string>> { }

        // Initializing class for the Forge Promotions JSON
        // JSON Structure:
        //  "promos": {
        //      "1.1-latest": "1.3.4.29",
        //      "1.2.3-latest": "1.4.1.64",
        //      "1.2.4-latest": "2.0.0.68",
        public class ForgePromotions
        {
            public Dictionary<string, string> promos { get; set; }
        }

        public UtilsService(UIService uiService, MainWindow mainWindow)
        {
            _uiService = uiService;
            _mainWindow = mainWindow;
            _httpClient = new HttpClient();
            _mainWindow = mainWindow;
        }

        // Helper method to ensure there's text in the first two inputs
        public static bool ValidateInputs(string folderPath, string mcVersion)
        {
            if (string.IsNullOrEmpty(mcVersion))
            {
                MessageBox.Show("Please select a Minecraft version.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show("Please select a valid modpack folder first.");
                return false;
            }

            return true;
        }
        // Helper method to see if Forge Server is already installed
        public bool ValidateServerInstallation(string folderPath)
        {
            string eulaPath = Path.Combine(folderPath, "eula.txt");

            // Checks if the eula.txt file exists and has been accepted
            if (File.Exists(eulaPath))
            {
                string eulaText = File.ReadAllText(eulaPath);
                if (eulaText.Contains("eula=true"))
                {
                    _uiService.UpdateInstallButtonState("✔ Installed");
                    return true;
                }
            }

            _uiService.UpdateInstallButtonState("Install Forge", true);
            return false;
        }
        // Helper to get the selected Forge version
        public async Task<string> GetSelectedForgeVersion()
        {
            string mcVersion = _mainWindow.cbMinecraftVersion.SelectedItem as string;
            var (latestStable, latestExperimental) = await GetLatestAvailableForgeVersionsAsync(mcVersion);

            if (_mainWindow.rbStable.IsChecked == false && _mainWindow.rbExperimental.IsChecked == false && _mainWindow.rbCustom.IsChecked == false)
                return "None";
            if (_mainWindow.rbStable.IsChecked == true) return latestStable;
            if (_mainWindow.rbExperimental.IsChecked == true) return latestExperimental;
            if (_mainWindow.rbCustom.IsChecked == true)
                return _mainWindow.cbCustomBuild.Text.Replace(" (Latest Build)", "");

            throw new InvalidOperationException("Could not determine Forge version.");
        }
        // Helper to get the selected server hosting method
        public string GetSelectedHost()
        {
            if (_mainWindow.rbNgrok.IsChecked == true) return "ngrok";
            if (_mainWindow.rbPlayit.IsChecked == true) return "playit";
            if (_mainWindow.rbPortForward.IsChecked == true) return "portforward";

            throw new InvalidOperationException("Could not determine the server host version.");
        }
        // Helper to look for any forge installer in the folder
        public string GetInstallerPath()
        {
            string folderPath = _mainWindow.txtModpackFolderPath.Text;
            return Directory.GetFiles(folderPath, "forge-*-installer.jar").First();
        }
        // Helper to fetch the already installed Forge version
        public (string mcVersion, string forgeVersion) GetInstalledVersion()
        {
            // Since Forge doesn't produce a manifest.json for easy version handling, this
            // basically uses the folder name inside the libraries folder, then it splits
            // it into two to get the necessary version strings
            string modpackPath = _mainWindow.txtModpackFolderPath.Text;
            string folderPath = Path.Combine(modpackPath, "libraries", "net", "minecraftforge", "forge");
            string folderName = Path.GetFileName(Directory.GetDirectories(folderPath).First());
            var parts = folderName.Split('-');
            string mcVersion = parts[0];
            string forgeVersion = parts[1];

            return (mcVersion, forgeVersion);
        }
        // Helper HttpClient for the download of files
        public async Task DownloadFileAsync(string downloadURL, string savePath)
        {
            try
            {
                using var response = await _httpClient.GetAsync(downloadURL, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading file: {ex.Message}");
            }
        }
        // Fetches the latest and stable versions of Forge
        public async Task<(string latestStable, string latestExperimental)> GetLatestAvailableForgeVersionsAsync(string mcVersion)
        {
            string url = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";

            try
            {
                string json = await _httpClient.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<ForgePromotions>(json);

                data.promos.TryGetValue($"{mcVersion}-recommended", out string stable);
                data.promos.TryGetValue($"{mcVersion}-latest", out string experimental);

                return (stable, experimental);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching Forge promotions: " + ex.Message);
                return (null, null);
            }
        }
        public async Task<List<string>> GetAllAvailableForgeVersionsAsync()
        {
            string selectedMcVersion = _mainWindow.cbMinecraftVersion.SelectedItem.ToString();

            try
            {
                string url = "https://files.minecraftforge.net/net/minecraftforge/forge/maven-metadata.json";

                string json = await _httpClient.GetStringAsync(url);
                var metadata = JsonConvert.DeserializeObject<ForgeMetadata>(json);

                if (metadata.TryGetValue(selectedMcVersion, out List<string> value))
                {
                    var forgeVersions = value.Select(v => v.Contains('-') ? v.Split('-')[1] : v) // Takes only the Forge build
                    .ToList();

                    // Appends " (Latest Build)" to the last item
                    if (forgeVersions.Count > 0)
                    {
                        int lastIndex = forgeVersions.Count - 1;
                        forgeVersions[lastIndex] = forgeVersions[lastIndex] + " (Latest Build)";
                    }

                    return forgeVersions;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch Forge versions: {ex.Message}");
                return null;
            }
        }
        public async Task<HashSet<string>> GetAvailableMcVersionsAsync()
        {
            // Preload Minecraft versions from promotions.json
            string url = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";

            string json = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<ForgePromotions>(json);

            // Extracts the unique MC versions from the keys
            var mcVersions = new HashSet<string>();
            foreach (var key in data.promos.Keys)
            {
                var mcVer = key.Split('-')[0]; // Parses it into something like "1.20.1"
                mcVersions.Add(mcVer);
            }

            return mcVersions;
        }
        public async Task PopulateCustomForgeBuildCombobox()
        {
            // Detects if there's text in the modpack path textbox, to populate with
            // the default Minecraft version's Forge builds, or with the 
            if (_mainWindow.txtModpackFolderPath.Text == null || _mainWindow.txtModpackFolderPath.Text == "")
            {
                var forgeVersions = await GetAllAvailableForgeVersionsAsync();

                _mainWindow.cbCustomBuild.ItemsSource = forgeVersions;
                _mainWindow.cbCustomBuild.SelectedIndex = forgeVersions.Count - 1; // default to latest
            }
            else
            {
                var forgeVersions = await GetAllAvailableForgeVersionsAsync();

                if (forgeVersions == null)
                {
                    _mainWindow.cbCustomBuild.ItemsSource = null;
                    _mainWindow.cbCustomBuild.Items.Clear();
                    _mainWindow.cbCustomBuild.Items.Add("No Forge builds found");
                    _mainWindow.cbCustomBuild.SelectedIndex = 0;
                }
                else
                {
                    _mainWindow.cbCustomBuild.ItemsSource = forgeVersions;
                    _mainWindow.cbCustomBuild.SelectedIndex = 0;

                    bool isInstalled = ValidateServerInstallation(_mainWindow.txtModpackFolderPath.Text);
                    if (isInstalled)
                    {
                        var (_, forgeVersion) = GetInstalledVersion();
                        _mainWindow.cbCustomBuild.SelectedItem = forgeVersion;
                    }
                }
            }
        }
        public async Task PreparePortForwardAsync()
        {
            _uiService.UpdateServerButtonState("Fetching Public IP...");

            // Gets the public IP from IPify.org and attaches the
            // default Minecraft server host Port (25565)
            string publicIp = await _httpClient.GetStringAsync("https://api.ipify.org");
            if (!string.IsNullOrWhiteSpace(publicIp))
            {
                string serverAddress = $"{publicIp.Trim()}:25565";
                _uiService.UpdateServerAddressText(serverAddress);
            }
            else
            {
                MessageBox.Show("Could not fetch your public IP. Please check your internet connection.");
            }
        }
    }
}
