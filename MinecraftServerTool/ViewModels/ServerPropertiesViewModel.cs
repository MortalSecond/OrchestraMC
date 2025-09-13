using MinecraftServerTool.Helpers;
using MinecraftServerTool.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MinecraftServerTool.ViewModels
{
    public class ServerPropertiesViewModel : INotifyPropertyChanged
    {
        // Internal variables for flow
        public event Action RestartRequired;
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isLoading = false;
        public ICommand SaveCommand { get; }

        // External variables from viewmodels
        public string ModpackPath { get; set; }


        // Actual server.properties fields
        private bool? _allowFlight;
        private bool? _allowNether;
        private bool? _commandBlocks;
        private string _difficulty;
        private bool? _hardcore;
        private bool? _pvp;
        private int? _maxPlayers;
        private bool? _onlineMode;
        private string _networkCompression;
        private string _levelName;
        private int? _maxWorldSize;
        private string _levelSeed;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (IsLoading == false)
                RestartRequired?.Invoke();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value;}
        }
        public ServerPropertiesViewModel()
        {
            SaveCommand = new RelayCommand(_ => SaveProperties());
        }
        private void SaveProperties()
        {
            ServerPropertiesService.SaveServerProperties(ModpackPath, this);
        }

        public bool? AllowFlight
        {
            get => _allowFlight;
            set { _allowFlight = value; OnPropertyChanged(nameof(AllowFlight)); }
        }

        public bool? AllowNether
        {
            get => _allowNether;
            set { _allowNether = value; OnPropertyChanged(nameof(AllowNether)); }
        }

        public bool? CommandBlocks
        {
            get => _commandBlocks;
            set { _commandBlocks = value; OnPropertyChanged(nameof(CommandBlocks)); }
        }

        public string Difficulty
        {
            get => _difficulty;
            set { _difficulty = value; OnPropertyChanged(nameof(Difficulty)); }
        }

        public bool? Hardcore
        {
            get => _hardcore;
            set { _hardcore = value; OnPropertyChanged(nameof(Hardcore)); }
        }

        public bool? Pvp
        {
            get => _pvp;
            set { _pvp = value; OnPropertyChanged(nameof(Pvp)); }
        }

        public int? MaxPlayers
        {
            get => _maxPlayers;
            set { _maxPlayers = value; OnPropertyChanged(nameof(MaxPlayers)); }
        }

        public bool? OnlineMode
        {
            get => _onlineMode;
            set { _onlineMode = value; OnPropertyChanged(nameof(OnlineMode)); }
        }

        public string NetworkCompression
        {
            get => _networkCompression;
            set { _networkCompression = value; OnPropertyChanged(nameof(NetworkCompression)); }
        }

        public string LevelName
        {
            get => _levelName;
            set { _levelName = value; OnPropertyChanged(nameof(LevelName)); }
        }

        public int? MaxWorldSize
        {
            get => _maxWorldSize;
            set { _maxWorldSize = value; OnPropertyChanged(nameof(MaxWorldSize)); }
        }

        public string LevelSeed
        {
            get => _levelSeed;
            set { _levelSeed = value; OnPropertyChanged(nameof(LevelSeed)); }
        }

        // Collections for ComboBoxes
        public ObservableCollection<string> Booleans { get; } =
            new ObservableCollection<string> { "Enabled", "Disabled" };

        public ObservableCollection<string> Difficulties { get; } =
            new ObservableCollection<string> { "Peaceful", "Easy", "Normal", "Hard" };

        public ObservableCollection<string> CompressionOptions { get; } =
            new ObservableCollection<string> { "Everything", "64", "128", "256", "512", "Disabled" };
    }
}
