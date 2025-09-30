using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace MinecraftServerTool.Services
{
    public class ServerService
    {
        private readonly UIService _uiService;
        private readonly UtilsService _utilsService;
        private readonly PlayitService _playitService;
        private readonly NgrokService _ngrokService;
        // Initializes class for the commandline of the
        // Forge Server JVM
        private Process minecraftServerProcess;

        public ServerService(UIService uiService, UtilsService utilsService, PlayitService playitService, NgrokService ngrokService)
        {
            _uiService = uiService;
            _utilsService = utilsService;
            _playitService = playitService;
            _ngrokService = ngrokService;
        }

        // Method to run the run.bat
        // This is called twice per new installation;
        // Once to generate the files, another to actually start the server
        public void RunServerOnce(string serverDirectory)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C run.bat",
                    WorkingDirectory = serverDirectory,
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
        // Changes the eula.txt to set it to "true"
        public static void AcceptEula(string serverDirectory)
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
        public void StartServer(string serverDirectory)
        {
            var (mcVersion, forgeVersion) = _utilsService.GetInstalledVersion();

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
        public void KillServer()
        {
            if (minecraftServerProcess != null && !minecraftServerProcess.HasExited)
            {
                // Sends "stop" command like it was typed in the JVM console
                minecraftServerProcess.StandardInput.WriteLine("stop");
                minecraftServerProcess.WaitForExit();
                minecraftServerProcess = null;
            }
        }
        public async Task BeginHostsAsync(string selectedHost, string binariesFolder, string modpackPath)
        {
            // Attempts to download and install the selected server host,
            // in case the user does not have them either in the binaries or in the modpack
            try
            {
                switch (selectedHost)
                {
                    case "ngrok":
                        _uiService.UpdateServerButtonState("Starting Ngrok...");
                        await _ngrokService.PrepareNgrokAsync(binariesFolder, modpackPath);
                        break;
                    case "playit":
                        _uiService.UpdateServerButtonState("Starting Playit...");
                        await _playitService.PreparePlayitAsync(binariesFolder, modpackPath);
                        break;
                    case "portforward":
                        _uiService.UpdateServerButtonState("Fetching Public IP...");
                        await _utilsService.PreparePortForwardAsync();
                        break;
                    default:
                        MessageBox.Show("Please select a valid server host option.");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing server host: {ex.Message}");
                _uiService.UpdateServerButtonState("Start Server", true);
                return;
            }
        }
        public void BeginServer(string modpackPath)
        {
            // Attempts to run the server proper
            try
            {
                _uiService.UpdateServerButtonState("Starting Server...");
                StartServer(modpackPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting up server: {ex.Message}");
            }
            finally
            {
                _uiService.UpdateServerButtonState("Stop Host", true);
            }
        }
        public void StopHosts(string selectedHost)
        {
            try
            {
                switch (selectedHost)
                {
                    case "ngrok":
                        _uiService.UpdateServerButtonState("Stopping Ngrok...");
                        _ngrokService.StopNgrok();
                        break;
                    case "playit":
                        _uiService.UpdateServerButtonState("Stopping Playit...");
                        _playitService.StopPlayit();
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
                _uiService.UpdateServerButtonState("Start Host", true);
            }
        }
    }
}
