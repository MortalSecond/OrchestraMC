using MinecraftServerTool.Services;
using MinecraftServerTool.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftServerTool
{
    public partial class MainWindow : Window
    {
        // Initializes classes for internal flow
        private readonly MainWindowViewModel mainVm;

        private readonly UIService _uiService;
        private readonly UtilsService _utilsService;
        private readonly PlayitService _playitService;
        private readonly NgrokService _ngrokService;
        private readonly ServerService _serverService;
        private readonly ForgeService _forgeService;
        public MainWindow()
        {
            InitializeComponent();
            _uiService = new UIService(this);
            _utilsService = new UtilsService(_uiService, this);
            _playitService = new PlayitService(_utilsService, _uiService);
            _ngrokService = new NgrokService(_utilsService, _uiService);
            _serverService = new ServerService(_uiService, _utilsService, _playitService, _ngrokService);
            _forgeService = new ForgeService(_uiService, _utilsService, _serverService);

            mainVm = new MainWindowViewModel();
            DataContext = mainVm;

            // Subscribe to RestartRequired
            mainVm.ServerProperties.RestartRequired += () =>
            {
                if (btnStartServer.Header.ToString() == "Stop Server")
                    _uiService.UpdateRestartButtonState("Restart Required", true);
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Populates the Minecraft Version combobox
            var mcVersions = await _utilsService.GetAvailableMcVersionsAsync();
            cbMinecraftVersion.ItemsSource = mcVersions.Reverse();
            cbMinecraftVersion.SelectedIndex = 0;

            // Disables all buttons to prevent exceptions at startup
            btnInstallForge.IsEnabled = false;
            btnStartServer.IsEnabled = false;
            btnRestartServer.IsEnabled = false;

            // Collapses the right column
            RightColumn.Width = new GridLength(0);

            // Auto-selects host and Forge version to prevent any exception errors.
            rbStable.IsChecked = true;
            rbPlayit.IsChecked = true;
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
            if (!UtilsService.ValidateInputs(modpackPath, mcVersion)) return;

            // Fetches the current Forge versions and then the selected one that will be used
            string forgeVersion = await _utilsService.GetSelectedForgeVersion();

            // Attempts to set up Forge Server and pauses until finished
            // But throws an error message if unsuccessful
            // Also updates the Install Forge button as per Task
            try
            {
                await _forgeService.PrepareForgeAsync(modpackPath, forgeVersion, mcVersion);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up Forge Server: {ex.Message}");
            }
            finally
            {
                _uiService.UpdateInstallButtonState("✔ Installed");
                _uiService.UpdateServerButtonState("Stop Host", true);
                _uiService.UpdateRestartButtonState("Restart Server", true);
            }
        }
        
        private async void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            string modpackPath = txtModpackFolderPath.Text.Trim();
            string binariesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "binaries");
            string selectedHost = _utilsService.GetSelectedHost();
            string buttonPrompt = btnStartServer.Header.ToString();

            switch (buttonPrompt)
            {
                case "Start Server":
                    await _serverService.BeginHostsAsync(selectedHost, binariesFolder, modpackPath);
                    _serverService.BeginServer(modpackPath);
                    break;
                case "Stop Host":
                    _serverService.StopHosts(selectedHost);
                    _uiService.UpdateServerAddressText("N/A");
                    break;
                case "Start Host":
                    await _serverService.BeginHostsAsync(selectedHost, binariesFolder, modpackPath);
                    break;
                default:
                    break;
            }
        }

        private async void btnRestartServer_Click(object sender, RoutedEventArgs e)
        {
            string modpackPath = txtModpackFolderPath.Text;

            if (btnRestartServer.Header.ToString() == "Restart Required")
                ServerPropertiesService.SaveServerProperties(modpackPath, mainVm.ServerProperties);

            _uiService.UpdateRestartButtonState("Saving...");
            _serverService.KillServer();
            await Task.Delay(5000);
            _uiService.UpdateRestartButtonState("Restarting...");
            _serverService.StartServer(modpackPath);
            await Task.Delay(5000);
            _uiService.UpdateRestartButtonState("Restart Server", true);
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

            await _utilsService.PopulateCustomForgeBuildCombobox();
        }

        // Enables or disables the custom Forge build combobox
        // Depending on which radiobox was selected
        private void rbCustom_Checked(object sender, RoutedEventArgs e)
        {
            cbCustomBuild.Visibility = Visibility.Visible;
            tbCustomBuild.Visibility = Visibility.Visible;
            tbCustomBuildBottom.Visibility = Visibility.Collapsed;
        }

        private void rbExperimental_Checked(object sender, RoutedEventArgs e)
        {
            cbCustomBuild.Visibility = Visibility.Collapsed;
            tbCustomBuild.Visibility = Visibility.Collapsed;
            tbCustomBuildBottom.Visibility = Visibility.Visible;
        }

        private void rbStable_Checked(object sender, RoutedEventArgs e)
        {
            cbCustomBuild.Visibility = Visibility.Collapsed;
            tbCustomBuild.Visibility = Visibility.Collapsed;
            tbCustomBuildBottom.Visibility = Visibility.Visible;
        }

        private void txtModpackFolderPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Changes the Install Forge button to "Installed" if eula.txt has been set to true
            // Also updates the Minecraft Version and Forge Version comboboxes to reflect this
            bool isInstalled = _utilsService.ValidateServerInstallation(txtModpackFolderPath.Text);

            if (isInstalled)
            {
                var (mcVersion, forgeVersion) = _utilsService.GetInstalledVersion();

                // Updates the comboboxes to reflect the versions
                cbMinecraftVersion.SelectedItem = mcVersion;
                rbCustom.IsChecked = true;
                rbCustom_Checked(sender, e);
                cbCustomBuild.SelectedItem = forgeVersion;

                // Makes all buttons and sidebars usable and visible
                spServerPropertiesPanel.Visibility = Visibility.Visible;
                spJavaArgumentsControl.Visibility = Visibility.Visible;
                RightColumn.Width = new GridLength(1, GridUnitType.Star);
                btnStartServer.IsEnabled = true;
                btnRestartServer.IsEnabled = true;

                var dict = ServerPropertiesService.ParseServerProperties(txtModpackFolderPath.Text);
                ServerPropertiesService.LoadServerProperties(dict, mainVm.ServerProperties);
            }
            if (isInstalled == false)
            {
                // Only enables the install forge, to telegraph to the user
                // that they should install the Forge Server first
                btnInstallForge.IsEnabled = true;
                spServerPropertiesPanel.Visibility = Visibility.Collapsed;
                spJavaArgumentsControl.Visibility = Visibility.Collapsed;
                btnStartServer.IsEnabled = false;
                btnRestartServer.IsEnabled = false;
            }
        }

        private void btnServerAddress_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(btnServerAddress.Content.ToString());
        }

        private void DropdownButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }

        // Validates absolutely everything so the user only has to click a single button
        // Installs server if needed, installs the tunnels if they're missing, or simply
        // starts up the server should everything already be in place
        private async void btnMasterRun_Click(object sender, RoutedEventArgs e)
        {
            string modpackPath = txtModpackFolderPath.Text;
            string mcVersion = cbMinecraftVersion.SelectedItem as string;

            if(btnMasterRun.Content.ToString() == "Run Server")
            {
                _uiService.UpdateMasterButtonState("Preparing...");

                _uiService.AppendOutputText("Validating Inputs...");
                bool isPopulated = UtilsService.ValidateInputs(modpackPath, mcVersion);
                if (isPopulated)
                {
                    _uiService.AppendOutputText("Checking if server is already installed...");
                    bool isInstalled = _utilsService.ValidateServerInstallation(modpackPath);
                    if (isInstalled)
                    {
                        _uiService.AppendOutputText("Starting the server...");
                        btnStartServer_Click(sender, e);
                    }
                    else
                    {
                        // Just a failsafe to autoselect the stable Forge version
                        // in case none of the radioboxes are checked
                        string selectedForgeVersion = await _utilsService.GetSelectedForgeVersion();
                        if (selectedForgeVersion == "None")
                            rbStable.IsChecked = true;

                        _uiService.AppendOutputText("Installing Forge...");
                        await _forgeService.PrepareForgeAsync(modpackPath, selectedForgeVersion, mcVersion);

                        _uiService.AppendOutputText("Starting the server...");
                        btnStartServer_Click(sender, e);
                    }
                    // Gives a 20 second buffer to let the server's JVM boot up
                    await Task.Delay(20000);
                    _uiService.UpdateMasterButtonState("Stop Server", true);
                    return;
                }
            }
            
            if(btnMasterRun.Content.ToString() == "Stop Server")
            {
                string selectedHost = _utilsService.GetSelectedHost();

                _uiService.AppendOutputText("Stopping the server...");
                _serverService.KillServer();
                _uiService.AppendOutputText("Stopping tunneling service...");
                _serverService.StopHosts(selectedHost);

                _uiService.UpdateMasterButtonState("Run Server", true);
                return;
            }
        }
    }
}