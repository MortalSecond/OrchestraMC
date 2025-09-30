using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace MinecraftServerTool.Services
{
    public class ForgeService
    {
        private readonly UIService _uiService;
        private readonly UtilsService _utilsService;
        private readonly ServerService _serverService;

        public ForgeService(UIService uiService, UtilsService utilsService, ServerService serverService)
        {
            _uiService = uiService;
            _utilsService = utilsService;
            _serverService = serverService;
        }

        public async Task DownloadForgeAsync(string folderPath, string forgeVersion, string mcVersion)
        {
            string savePath = Path.Combine(folderPath, $"forge-{forgeVersion}-installer.jar");
            string versionString = $"{mcVersion}-{forgeVersion}";
            string downloadURL = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{versionString}/forge-{versionString}-installer.jar";

            await _utilsService.DownloadFileAsync(downloadURL, savePath);
        }
        public async Task InstallForgeServerAsync(string forgeInstallerPath, string targetDirectory)
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

            // Subscribe to output
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lock (_uiService._bufferLock)
                    {
                        _uiService._outputBuffer.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    lock (_uiService._bufferLock)
                    {
                        _uiService._outputBuffer.AppendLine("[ERROR] " + e.Data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }

        public async Task PrepareForgeAsync(string modpackPath, string forgeVersion, string mcVersion)
        {
            _uiService.UpdateInstallButtonState("Downloading...");
            await DownloadForgeAsync(modpackPath, forgeVersion, mcVersion);

            _uiService.UpdateInstallButtonState("Installing...");
            string installerPath = _utilsService.GetInstallerPath();
            await InstallForgeServerAsync(installerPath, modpackPath);

            // Does some run.bat shenanigans to properly set up the server files
            _uiService.UpdateInstallButtonState("Generating Modpack Files...");
            _uiService.AppendOutputText("Generating Files...");
            // Step 1: Run once to generate eula.txt
            _serverService.RunServerOnce(modpackPath);
            _uiService.UpdateInstallButtonState("Accepting EULA...");
            _uiService.AppendOutputText("Accepting EULA...");
            // Step 2: Change the EULA's text file to true
            ServerService.AcceptEula(modpackPath);
            _uiService.UpdateInstallButtonState("Generating Server Files...");
            _uiService.AppendOutputText("Generating Files...");
            // Step 3: Run once more to generate the rest of the files
            _serverService.StartServer(modpackPath);
        }
    }
}
