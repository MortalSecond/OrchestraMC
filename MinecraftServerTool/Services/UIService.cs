using System;
using System.Text;
using System.Windows.Threading;

namespace MinecraftServerTool.Services
{
    public class UIService
    {

        // Initializes classes for the output textbox
        public readonly StringBuilder _outputBuffer = new StringBuilder();
        public readonly object _bufferLock = new object();
        public DispatcherTimer _flushTimer;
        private readonly MainWindow _mainWindow;
        // Since UIService is no longer inheriting Window from the XAML,
        // this class helps bind the Dispatcher correctly
        public Dispatcher UIDispatcher => _mainWindow.Dispatcher;

        public UIService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            _flushTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _flushTimer.Tick += (s, e) => FlushOutputToUI();
            _flushTimer.Start();
        }

        // Updates the Install Forge button depending on task
        public void UpdateInstallButtonState(string text, bool enabled = false)
        {
            _mainWindow.btnInstallForge.IsEnabled = enabled;
            _mainWindow.btnInstallForge.Header = text;
        }
        // Updates the Start Server button depending on task
        public void UpdateServerButtonState(string text, bool enabled = false)
        {
            _mainWindow.btnStartServer.IsEnabled = enabled;
            _mainWindow.btnStartServer.Header = text;
        }
        // Updates the Restart Server button depending on server status
        public void UpdateRestartButtonState(string text, bool enabled = false)
        {
            _mainWindow.btnRestartServer.IsEnabled = enabled;
            _mainWindow.btnRestartServer.Header = text;
        }
        // Updates the big 'master' button
        public void UpdateMasterButtonState(string text, bool enabled = false)
        {
            _mainWindow.btnMasterRun.Content = text;
            _mainWindow.btnMasterRun.IsEnabled = enabled;
        }
        // Updates the Server Adress textblock to the public URL
        public void UpdateServerAddressText(string text)
        {
            _mainWindow.btnServerAddress.Content = text;
        }
        // Appends text to the output window
        public void AppendOutputText(string text)
        {
            _mainWindow.txtDebugOutput.AppendText(text + Environment.NewLine);
            _mainWindow.txtDebugOutput.ScrollToEnd();
        }
        public void FlushOutputToUI()
        {
            string textToAppend;

            lock (_bufferLock)
            {
                if (_outputBuffer.Length == 0) return;
                textToAppend = _outputBuffer.ToString();
                _outputBuffer.Clear();
            }

            _mainWindow.txtDebugOutput.AppendText(textToAppend);
            _mainWindow.txtDebugOutput.ScrollToEnd();
        }
    }
}
