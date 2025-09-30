using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MinecraftServerTool.Services
{
    public class PlayitService
    {
        private UtilsService _utilsService;
        private UIService _uiService;
        private Process playitProcess;
        private TaskCompletionSource<string> _tunnelAddressTcs;

        public PlayitService(UtilsService utilsService, UIService uiService)
        {
            _utilsService = utilsService;
            _uiService = uiService;
        }

        // I wanted to separate StartPlayit and GetPlayitAddress separated,
        // but the cmd process variable is such a headache -- so this will
        // provide the server address string for sanity's sake
        public async Task<string> StartPlayit(string playitModpackPath, string modpackPath)
        {
            _tunnelAddressTcs = new TaskCompletionSource<string>();

            playitProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = playitModpackPath,
                    WorkingDirectory = modpackPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                }
            };

            bool inTunnelsSection = false;

            // Subscribe to output
            playitProcess.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;

                // Only log if we haven’t already found a tunnel
                if (!_tunnelAddressTcs.Task.IsCompleted)
                {
                    _uiService.UIDispatcher.BeginInvoke(new Action(() =>
                    {
                        _uiService.AppendOutputText(e.Data);
                    }));
                }

                if (e.Data.Trim().Equals("TUNNELS", StringComparison.OrdinalIgnoreCase))
                {
                    inTunnelsSection = true;
                }
                else if (inTunnelsSection && e.Data.Contains("=>"))
                {
                    var parts = e.Data.Split(["=>"], StringSplitOptions.None);
                    if (parts.Length > 0)
                    {
                        _tunnelAddressTcs.TrySetResult(parts[0].Trim());
                    }
                }
            };

            playitProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _uiService.UIDispatcher.BeginInvoke(new Action(() =>
                    {
                        _uiService.AppendOutputText(e.Data);
                    }));
                }
            };

            // Starts the Playit tunnel and gives 15 seconds of buffer
            playitProcess.Start();
            playitProcess.BeginOutputReadLine();
            playitProcess.BeginErrorReadLine();
            var completedTask = await Task.WhenAny(_tunnelAddressTcs.Task, Task.Delay(15000));
            return completedTask == _tunnelAddressTcs.Task ? await _tunnelAddressTcs.Task : null;
        }
        public async Task DownloadPlayitAsync(string playitBinariesPath)
        {
            string downloadURL = "https://github.com/playit-cloud/playit-agent/releases/download/v0.15.26/playit-windows-x86_64-signed.exe";

            await _utilsService.DownloadFileAsync(downloadURL, playitBinariesPath);
        }
        public static void InstallPlayit(string modpackPath, string playitBinariesPath)
        {
            // Copies ngrok.exe into modpack folder
            string targetPath = Path.Combine(modpackPath, "playit.exe");
            File.Copy(playitBinariesPath, targetPath, overwrite: true);
        }
        public async Task PreparePlayitAsync(string binariesFolder, string modpackPath)
        {
            string playitBinariesPath = Path.Combine(binariesFolder, "playit.exe");
            string playitModpackPath = Path.Combine(modpackPath, "playit.exe");

            bool isPlayitDownloaded = File.Exists(playitBinariesPath);
            bool isPlayitInstalled = File.Exists(playitModpackPath);

            // Download Playit if not already present
            if (isPlayitDownloaded == false)
            {
                _uiService.UpdateServerButtonState("Downloading Playit...");
                await DownloadPlayitAsync(playitBinariesPath);
            }
            // Copy to modpack folder if not installed
            if (isPlayitInstalled == false)
            {
                _uiService.UpdateServerButtonState("Installing Playit...");
                InstallPlayit(modpackPath, playitBinariesPath);
            }

            // Starts the Playit.gg tunnel, fetches the public address,
            // and updates the textblock to reflect that
            _uiService.UpdateServerButtonState("Starting Playit.gg...");
            string serverAddress = await StartPlayit(playitModpackPath, modpackPath);
            if (!string.IsNullOrEmpty(serverAddress))
            {
                _uiService.UpdateServerAddressText(serverAddress);
            }
        }
        public void StopPlayit()
        {
            if (playitProcess != null && !playitProcess.HasExited)
            {
                playitProcess.Kill();
                playitProcess = null;
            }
        }
    }
}
