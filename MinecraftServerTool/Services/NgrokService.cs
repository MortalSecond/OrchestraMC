using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace MinecraftServerTool.Services
{
    public class NgrokService
    {
        private UtilsService _utilsService;
        private UIService _uiService;
        private readonly HttpClient _httpClient;
        private Process ngrokProcess;

        public NgrokService(UtilsService utilsService, UIService uiService)
        {
            _httpClient = new HttpClient();
            _utilsService = utilsService;
            _uiService = uiService;
        }

        // Fetches the public URL from Ngrok's API
        public async Task<string> GetNgrokAddressAsync()
        {
            string response = await _httpClient.GetStringAsync("http://127.0.0.1:4040/api/tunnels");
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
            return null;
        }
        public void StartNgrok(string modpackPath)
        {
            // I removed the output logging from Ngrok because ngrok.exe doesn't produce an stdout.
            // Kinda sucks, but that's how it'll be until i can find a proper way to log it.
            ngrokProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(modpackPath, "ngrok.exe"),
                    Arguments = "tcp 25565",
                    WorkingDirectory = modpackPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
        }
        public async Task DownloadNgrokAsync(string binariesPath)
        {
            string ngrokZipPath = Path.Combine(binariesPath, "ngrok.zip");

            // Downloads Ngrok
            string downloadURL = "https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-windows-amd64.zip";
            await _utilsService.DownloadFileAsync(downloadURL, ngrokZipPath);

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
        public static void InstallNgrok(string modpackFolder, string ngrokBinariesPath)
        {
            // Copies ngrok.exe into modpack folder
            string targetPath = Path.Combine(modpackFolder, "ngrok.exe");
            File.Copy(ngrokBinariesPath, targetPath, overwrite: true);
        }
        public async Task PrepareNgrokAsync(string binariesFolder, string modpackPath)
        {
            string ngrokBinariesPath = Path.Combine(binariesFolder, "Ngrok.exe");
            string ngrokModpackPath = Path.Combine(modpackPath, "Ngrok.exe");

            bool isNgrokDownloaded = File.Exists(ngrokBinariesPath);
            bool isNgrokInstalled = File.Exists(ngrokModpackPath);

            // Downloads Ngrok and unzips it into the binaries
            if (isNgrokDownloaded == false)
            {
                _uiService.UpdateServerButtonState("Downloading Ngrok...");
                await DownloadNgrokAsync(ngrokBinariesPath);
            }
            // Copies and pastes Ngrok from the binaries into the modpack
            if (isNgrokInstalled == false)
            {
                _uiService.UpdateServerButtonState("Installing Ngrok...");
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
                _uiService.UpdateServerAddressText(serverAddress);
            }
        }
        public void StopNgrok()
        {
            if (ngrokProcess != null && !ngrokProcess.HasExited)
            {
                ngrokProcess.Kill();
                ngrokProcess = null;
            }
        }
    }
}
