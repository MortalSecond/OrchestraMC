using MinecraftServerTool.Controls;
using MinecraftServerTool.ViewModels;
using MinecraftServerTool.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftServerTool
{
    public partial class MainWindow : Window
    {
        private readonly ServerPropertiesService serverPropertiesService;
        private readonly MainWindowViewModel mainVm;
        public MainWindow()
        {
            InitializeComponent();
            mainVm = new MainWindowViewModel();
            DataContext = mainVm;

            // Subscribe to RestartRequired
            mainVm.ServerProperties.RestartRequired += () =>
            {
                UpdateRestartButtonState("Restart Required", true);
            };
        }
        // Initializes class for the commandline of the
        // Forge Server JVM
        private Process minecraftServerProcess;
        private Process ngrokProcess;
        private Process playitProcess;
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

        // Helper method to ensure there's text in the first two inputs
        private bool ValidateInputs(string folderPath, string mcVersion)
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
        private bool ValidateServerInstallation(string folderPath)
        {
            string eulaPath = Path.Combine(folderPath, "eula.txt");

            // Checks if the eula.txt file exists and has been accepted
            if (File.Exists(eulaPath))
            {
                string eulaText = File.ReadAllText(eulaPath);
                if (eulaText.Contains("eula=true"))
                {
                    UpdateInstallButtonState("✔ Installed");
                    return true;
                }
            }

            UpdateInstallButtonState("Install Forge", true);
            return false;
        }
        // Helper to get the selected Forge version
        private async Task<string> GetSelectedForgeVersion()
        {
            string mcVersion = cbMinecraftVersion.SelectedItem as string;
            var (latestStable, latestExperimental) = await GetLatestAvailableForgeVersionsAsync(mcVersion);

            if (rbStable.IsChecked == true) return latestStable;
            if (rbExperimental.IsChecked == true) return latestExperimental;
            if (rbCustom.IsChecked == true)
                return cbCustomBuild.Text.Replace(" (Latest Build)", "");

            throw new InvalidOperationException("Could not determine Forge version.");
        }
        // Helper to get the selected server hosting method
        private string GetSelectedHost()
        {
            if (rbNgrok.IsChecked == true) return "ngrok";
            if (rbPlayit.IsChecked == true) return "playit";
            if (rbPortForward.IsChecked == true) return "portforward";

            throw new InvalidOperationException("Could not determine the server host version.");
        }
        // Helper to look for any forge installer in the folder
        private string GetInstallerPath()
        {
            string folderPath = txtModpackFolderPath.Text;
            return Directory.GetFiles(folderPath, "forge-*-installer.jar").First();
        }
        // Helper to fetch the already installed Forge version
        private (string mcVersion, string forgeVersion) GetInstalledVersion()
        {
            // Since Forge doesn't produce a manifest.json for easy version handling, this
            // basically uses the folder name inside the libraries folder, then it splits
            // it into two to get the necessary version strings
            string modpackPath = txtModpackFolderPath.Text;
            string folderPath = Path.Combine(modpackPath, "libraries", "net", "minecraftforge", "forge");
            string folderName = Path.GetFileName(Directory.GetDirectories(folderPath).First());
            var parts = folderName.Split('-');
            string mcVersion = parts[0];
            string forgeVersion = parts[1];

            return (mcVersion, forgeVersion);
        }
        // Fetches the latest and stable versions of Forge
        private async Task<(string latestStable, string latestExperimental)> GetLatestAvailableForgeVersionsAsync(string mcVersion)
        {
            string url = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";

            using (var client = new WebClient())
            {
                try
                {
                    string json = await client.DownloadStringTaskAsync(url);
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
        }
        private async Task<List<string>> GetAllAvailableForgeVersionsAsync()
        {
            string selectedMcVersion = cbMinecraftVersion.SelectedItem.ToString();

            try
            {
                string url = "https://files.minecraftforge.net/net/minecraftforge/forge/maven-metadata.json";
                using (var client = new WebClient())
                {
                    string json = await client.DownloadStringTaskAsync(url);
                    var metadata = JsonConvert.DeserializeObject<ForgeMetadata>(json);

                    if (metadata.ContainsKey(selectedMcVersion))
                    {
                        var forgeVersions = metadata[selectedMcVersion]
                        .Select(v => v.Contains("-") ? v.Split('-')[1] : v) // Takes only the Forge build
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to fetch Forge versions: {ex.Message}");
                return null;
            }
        }
        private async Task<HashSet<string>> GetAvailableMcVersionsAsync()
        {
            // Preload Minecraft versions from promotions.json
            string url = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";
            using (var client = new WebClient())
            {
                string json = await client.DownloadStringTaskAsync(url);
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
        }
        private async Task PopulateCustomForgeBuildCombobox()
        {
            // Detects if there's text in the modpack path textbox, to populate with
            // the default Minecraft version's Forge builds, or with the 
            if (txtModpackFolderPath.Text == null || txtModpackFolderPath.Text == "")
            {
                var forgeVersions = await GetAllAvailableForgeVersionsAsync();

                cbCustomBuild.ItemsSource = forgeVersions;
                cbCustomBuild.SelectedIndex = forgeVersions.Count - 1; // default to latest
            }
            else
            {
                var forgeVersions = await GetAllAvailableForgeVersionsAsync();

                if (forgeVersions == null)
                {
                    cbCustomBuild.ItemsSource = null;
                    cbCustomBuild.Items.Clear();
                    cbCustomBuild.Items.Add("No Forge builds found");
                    cbCustomBuild.SelectedIndex = 0;
                }
                else
                {
                    var (mcVersion, forgeVersion) = GetInstalledVersion();
                    cbCustomBuild.ItemsSource = forgeVersions;
                    cbCustomBuild.SelectedItem = forgeVersion;
                }
            }
        }
        // Fetches the public URL from Ngrok's API
        private async Task<string> GetNgrokAddressAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string response = await client.GetStringAsync("http://127.0.0.1:4040/api/tunnels");
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                // Get the first TCP tunnel
                foreach (var tunnel in json.tunnels)
                {
                    if ((string)tunnel.proto == "tcp")
                    {
                        // The output has a prefix that messes with the usable URL,
                        // so it has to be trimmed to be properly usable
                        string forwarding = tunnel.public_url;
                        return forwarding.Replace("tcp://", "");
                    }
                }
            }
            return null;
        }
        private void StartNgrok(string modpackPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(modpackPath, "ngrok.exe"),
                Arguments = "tcp 25565",
                WorkingDirectory = modpackPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            ngrokProcess = new Process { StartInfo = psi };
            ngrokProcess.Start();
        }
        private async Task DownloadNgrokAsync(string binariesPath)
        {
            string ngrokZipPath = Path.Combine(binariesPath, "ngrok.zip");

            // Downloads Ngrok
            string downloadURL = "https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-windows-amd64.zip";
            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(new Uri(downloadURL), ngrokZipPath);
            }

            // Extracts the ngrok.exe into binaries folder
            string ngrokExePath = Path.Combine(binariesPath, "ngrok.exe");
            using (ZipArchive archive = ZipFile.OpenRead(ngrokZipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("ngrok.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(ngrokExePath, overwrite: true);
                        break;
                    }
                }
            }

            // Deletes the ZIP to keep things clean
            File.Delete(ngrokZipPath);
        }
        private void InstallNgrok(string modpackFolder, string ngrokBinariesPath)
        {
            // Copies ngrok.exe into modpack folder
            string targetPath = Path.Combine(modpackFolder, "ngrok.exe");
            File.Copy(ngrokBinariesPath, targetPath, overwrite: true);
        }
        private async Task PrepareNgrokAsync(string binariesFolder, string modpackPath)
        {
            string ngrokBinariesPath = Path.Combine(binariesFolder, "Ngrok.exe");
            string ngrokModpackPath = Path.Combine(modpackPath, "Ngrok.exe");

            bool isNgrokDownloaded = File.Exists(ngrokBinariesPath);
            bool isNgrokInstalled = File.Exists(ngrokModpackPath);

            // Downloads Ngrok and unzips it into the binaries
            if (isNgrokDownloaded == false)
            {
                UpdateServerButtonState("Downloading Ngrok...");
                await DownloadNgrokAsync(ngrokBinariesPath);
            }
            // Copies and pastes Ngrok from the binaries into the modpack
            if (isNgrokInstalled == false)
            {
                UpdateServerButtonState("Installing Ngrok...");
                InstallNgrok(modpackPath, ngrokBinariesPath);
            }

            // Starts the Ngrok tunnel and gives 2 seconds of buffer
            // Just so the API has time to initialize
            StartNgrok(modpackPath);
            Task.Delay(2000).Wait();

            // Fetches the public URL and updates the textblock to reflect that
            string serverAddress = await GetNgrokAddressAsync();
            if (!string.IsNullOrEmpty(serverAddress))
            {
                UpdateServerAddressText(serverAddress);
            }
        }
        private void StopNgrok()
        {
            if (ngrokProcess != null && !ngrokProcess.HasExited)
            {
                ngrokProcess.Kill();
                ngrokProcess = null;
            }
        }
        // I wanted to separate StartPlayit and GetPlayitAddress separated,
        // but the cmd process variable is such a headache -- so this will
        // provide the server address string for sanity's sake
        private async Task<string> StartPlayit(string playitModpackPath, string modpackPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = playitModpackPath,
                WorkingDirectory = modpackPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            playitProcess = new Process { StartInfo = psi };

            using (playitProcess)
            {
                // Starts the Playit tunnel and gives 15 seconds of buffer
                playitProcess = new Process { StartInfo = psi };
                playitProcess.Start();
                await Task.Delay(15000);

                string line;
                bool inTunnelsSection = false;

                // Since Playit.gg provides a very barebones cmd.exe output, this
                // attempts to extract the public address. It comes after "TUNNELS"
                // and before a "=> 127.x.x.x:xxxx" -- so it finds it and splits it
                while ((line = await playitProcess.StandardOutput.ReadLineAsync()) != null)
                {
                    if (line.Trim().Equals("TUNNELS", StringComparison.OrdinalIgnoreCase))
                    {
                        inTunnelsSection = true;
                        continue;
                    }

                    if (inTunnelsSection && line.Contains("=>"))
                    {
                        var parts = line.Split(new[] { "=>" }, StringSplitOptions.None);
                        if (parts.Length > 0)
                        {
                            return parts[0].Trim();
                        }
                    }
                }
            }

            return null;
        }
        private async Task DownloadPlayitAsync(string playitBinariesPath)
        {
            string downloadUrl = "https://github.com/playit-cloud/playit-agent/releases/download/v0.15.26/playit-windows-x86_64-signed.exe";
            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(new Uri(downloadUrl), playitBinariesPath);
            }
        }
        private void InstallPlayit(string modpackPath, string playitBinariesPath)
        {
            // Copies ngrok.exe into modpack folder
            string targetPath = Path.Combine(modpackPath, "playit.exe");
            File.Copy(playitBinariesPath, targetPath, overwrite: true);
        }
        private async Task PreparePlayitAsync(string binariesFolder, string modpackPath)
        {
            string playitBinariesPath = Path.Combine(binariesFolder, "playit.exe");
            string playitModpackPath = Path.Combine(modpackPath, "playit.exe");

            bool isPlayitDownloaded = File.Exists(playitBinariesPath);
            bool isPlayitInstalled = File.Exists(playitModpackPath);

            // Download Playit if not already present
            if (isPlayitDownloaded == false)
            {
                UpdateServerButtonState("Downloading Playit...");
                await DownloadPlayitAsync(playitBinariesPath);
            }
            // Copy to modpack folder if not installed
            if (isPlayitInstalled == false)
            {
                UpdateServerButtonState("Installing Playit...");
                InstallPlayit(modpackPath, playitBinariesPath);
            }

            // Starts the Playit.gg tunnel, fetches the public address,
            // and updates the textblock to reflect that
            UpdateServerButtonState("Starting Playit.gg...");
            string serverAddress = await StartPlayit(playitModpackPath, modpackPath);
            if (!string.IsNullOrEmpty(serverAddress))
            {
                UpdateServerAddressText(serverAddress);
            }
        }
        private void StopPlayit()
        {
            if (playitProcess != null && !playitProcess.HasExited)
            {
                playitProcess.Kill();
                playitProcess = null;
            }
        }
        private async Task DownloadForgeAsync(string folderPath, string forgeVersion, string mcVersion)
        {
            string savePath = Path.Combine(folderPath, $"forge-{forgeVersion}-installer.jar");
            string versionString = $"{mcVersion}-{forgeVersion}";
            string downloadUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{versionString}/forge-{versionString}-installer.jar";

            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(new Uri(downloadUrl), savePath);
            }
        }
        private void InstallForgeServer(string forgeInstallerPath, string targetDirectory)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{forgeInstallerPath}\" --installServer",
                    WorkingDirectory = targetDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine("ERR: " + e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
        private async Task PrepareForgeAsync(string modpackPath, string forgeVersion, string mcVersion)
        {
            UpdateInstallButtonState("Downloading...");
            await DownloadForgeAsync(modpackPath, forgeVersion, mcVersion);

            UpdateInstallButtonState("Installing...");
            string installerPath = GetInstallerPath();
            await Task.Run(() => InstallForgeServer(installerPath, modpackPath));

            // Does some run.bat shenanigans to properly set up the server files
            UpdateInstallButtonState("Generating Files...");
            await Task.Run(() =>
            {
                // Step 1: Run once to generate eula.txt
                RunServerOnce(modpackPath);
                UpdateInstallButtonState("Accepting EULA...");
                // Step 2: Change the EULA's text file to true
                AcceptEula(modpackPath);
                UpdateInstallButtonState("Processing...");
                // Step 3: Run once more to generate the rest of the files
                RunServerOnce(modpackPath);
            });
        }
        private async Task PreparePortForwardAsync()
        {
            UpdateServerButtonState("Fetching Public IP...");

            // Gets the public IP from IPify.org and attaches the
            // default Minecraft server host Port (25565)
            using (var client = new WebClient())
            {
                string publicIp = await client.DownloadStringTaskAsync("https://api.ipify.org");
                if (!string.IsNullOrWhiteSpace(publicIp))
                {
                    string serverAddress = $"{publicIp.Trim()}:25565";
                    UpdateServerAddressText(serverAddress);
                }
                else
                {
                    MessageBox.Show("Could not fetch your public IP. Please check your internet connection.");
                }
            }
        }

        // Method to run the run.bat
        // This is called twice per new installation;
        // Once to generate the files, another to actually start the server
        private void RunServerOnce(string serverDirectory)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C run.bat",
                WorkingDirectory = serverDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process != null)
                {
                    // Give the server 10 secs to generate eula.txt
                    process.WaitForExit(10000);
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }
        }
        // Changes the eula.txt to set it to "true"
        private void AcceptEula(string serverDirectory)
        {
            string eulaPath = Path.Combine(serverDirectory, "eula.txt");
            if (File.Exists(eulaPath))
            {
                string text = File.ReadAllText(eulaPath);
                text = text.Replace("eula=false", "eula=true");
                File.WriteAllText(eulaPath, text);
            }
        }
        // Method to run the run.bat without killing the process,
        // to actually start the server
        private void StartServer(string serverDirectory)
        {
            var (mcVersion, forgeVersion) = GetInstalledVersion();

            // For context: This doesn't run the run.bat anymore, this directly
            // runs the argument file from the libraries, otherwise the commandline
            // isn't able to be read by the program and thus can't be sent inputs
            var psi = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"@user_jvm_args.txt @libraries/net/minecraftforge/forge/{mcVersion}-{forgeVersion}/win_args.txt",
                WorkingDirectory = serverDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };

            minecraftServerProcess = Process.Start(psi);
        }
        private void KillServer()
        {
            if (minecraftServerProcess != null && !minecraftServerProcess.HasExited)
            {
                // Sends "stop" command like it was typed in the JVM console
                minecraftServerProcess.StandardInput.WriteLine("stop");
                minecraftServerProcess.WaitForExit();
                minecraftServerProcess = null;
            }
        }
        private async Task BeginHostsAsync(string selectedHost, string binariesFolder, string modpackPath)
        {
            // Attempts to download and install the selected server host,
            // in case the user does not have them either in the binaries or in the modpack
            try
            {
                switch (selectedHost)
                {
                    case "ngrok":
                        UpdateServerButtonState("Starting Ngrok...");
                        await PrepareNgrokAsync(binariesFolder, modpackPath);
                        break;
                    case "playit":
                        UpdateServerButtonState("Starting Playit...");
                        await PreparePlayitAsync(binariesFolder, modpackPath);
                        break;
                    case "portforward":
                        UpdateServerButtonState("Fetching Public IP...");
                        await PreparePortForwardAsync();
                        break;
                    default:
                        MessageBox.Show("Please select a valid server host option.");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing server host: {ex.Message}");
                UpdateServerButtonState("Start Server", true);
                return;
            }
        }
        private void BeginServer(string modpackPath)
        {
            // Attempts to run the server proper
            try
            {
                UpdateServerButtonState("Starting Server...");
                StartServer(modpackPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting up server: {ex.Message}");
            }
            finally
            {
                UpdateServerButtonState("Stop Host", true);
            }
        }
        private void StopHosts(string selectedHost)
        {
            try
            {
                switch (selectedHost)
                {
                    case "ngrok":
                        UpdateServerButtonState("Stopping Ngrok...");
                        StopNgrok();
                        break;
                    case "playit":
                        UpdateServerButtonState("Stopping Playit...");
                        StopPlayit();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping server process: {ex.Message}");
            }
            finally
            {
                UpdateServerButtonState("Start Host", true);
            }
        }

        // Updates the Install Forge button depending on task
        private void UpdateInstallButtonState(string text, bool enabled = false)
        {
            btnInstallForge.IsEnabled = enabled;
            btnInstallForge.Content = text;
        }
        // Updates the Start Server button depending on task
        private void UpdateServerButtonState(string text, bool enabled = false)
        {
            btnStartServer.IsEnabled = enabled;
            btnStartServer.Content = text;
        }
        // Updates the Restart Server button depending on server status
        private void UpdateRestartButtonState(string text, bool enabled = false)
        {
            btnRestartServer.IsEnabled = enabled;
            btnRestartServer.Content = text;
        }
        // Updates the Server Adress textblock to the public URL
        private void UpdateServerAddressText(string text)
        {
            btnServerAddress.Content = text;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Populates the Minecraft Version combobox
            var mcVersions = await GetAvailableMcVersionsAsync();
            cbMinecraftVersion.ItemsSource = mcVersions.Reverse();
            cbMinecraftVersion.SelectedIndex = 0;
        }

        private void btnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            // Shows the File Explorer
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = dlg.SelectedPath;
                txtModpackFolderPath.Text = folderPath;
            }
        }

        private async void btnInstallForge_Click(object sender, RoutedEventArgs e)
        {
            // Fetches the modpack path and MC version, then validates
            string modpackPath = txtModpackFolderPath.Text;
            string mcVersion = cbMinecraftVersion.SelectedItem as string;
            if (!ValidateInputs(modpackPath, mcVersion)) return;

            // Fetches the current Forge versions and then the selected one that will be used
            string forgeVersion = await GetSelectedForgeVersion();

            // Attempts to set up Forge Server and pauses until finished
            // But throws an error message if unsuccessful
            // Also updates the Install Forge button as per Task
            try
            {
                await PrepareForgeAsync(modpackPath, forgeVersion, mcVersion);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up Forge Server: {ex.Message}");
            }
            finally
            {
                UpdateInstallButtonState("✔ Installed");
            }
        }
        
        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            string modpackPath = txtModpackFolderPath.Text.Trim();
            string binariesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "binaries");
            string selectedHost = GetSelectedHost();
            string buttonPrompt = btnStartServer.Content.ToString();

            switch (buttonPrompt)
            {
                case "Start Server":
                    await BeginHostsAsync(selectedHost, binariesFolder, modpackPath);
                    BeginServer(modpackPath);
                    break;
                case "Stop Host":
                    StopHosts(selectedHost);
                    break;
                case "Start Host":
                    await BeginHostsAsync(selectedHost, binariesFolder, modpackPath);
                    break;
                default:
                    break;
            }
        }

        private async void btnRestartServer_Click(object sender, RoutedEventArgs e)
        {
            string modpackPath = txtModpackFolderPath.Text;

            if (btnRestartServer.Content.ToString() == "Restart Required")
                ServerPropertiesService.SaveServerProperties(modpackPath, mainVm.ServerProperties);

            UpdateRestartButtonState("Saving...");
            KillServer();
            await Task.Delay(5000);
            UpdateRestartButtonState("Restarting...");
            StartServer(modpackPath);
            await Task.Delay(5000);
            UpdateRestartButtonState("Restart Server", true);
        }

        // Change the Forge versions to fit the currently selected Minecraft version
        // CURRENTLY?!
        // CURRENTLYPENDING?!
        // No way!!
        // Sorry.
        private async void cbMinecraftVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbMinecraftVersion.SelectedItem == null)
                return;

            await PopulateCustomForgeBuildCombobox();
        }

        // Enables or disables the custom Forge build combobox
        // Depending on which radiobox was selected
        private void rbCustom_Checked(object sender, RoutedEventArgs e)
        {
            cbCustomBuild.Visibility = Visibility.Visible;
        }

        private void rbExperimental_Checked(object sender, RoutedEventArgs e)
        {
            cbCustomBuild.Visibility = Visibility.Collapsed;
        }

        private void rbStable_Checked(object sender, RoutedEventArgs e)
        {
            cbCustomBuild.Visibility = Visibility.Collapsed;
        }

        private void txtModpackFolderPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Changes the Install Forge button to "Installed" if eula.txt has been set to true
            // Also updates the Minecraft Version and Forge Version comboboxes to reflect this
            bool isInstalled = ValidateServerInstallation(txtModpackFolderPath.Text);

            if (isInstalled)
            {
                var (mcVersion, forgeVersion) = GetInstalledVersion();

                // Updates the comboboxes to reflect the versions
                cbMinecraftVersion.SelectedItem = mcVersion;
                rbCustom.IsChecked = true;
                rbCustom_Checked(sender, e);
                cbCustomBuild.SelectedItem = forgeVersion;

                spServerPropertiesPanel.Visibility = Visibility.Visible;

                var dict = ServerPropertiesService.ParseServerProperties(txtModpackFolderPath.Text);
                ServerPropertiesService.LoadServerProperties(dict, mainVm.ServerProperties);
            }
        }

        private void btnServerAddress_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(btnServerAddress.Content.ToString());
        }
    }
}