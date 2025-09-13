using System.ComponentModel;

namespace MinecraftServerTool.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _modpackPath;
        public string ModpackPath
        {
            get => _modpackPath;
            set
            {
                if (_modpackPath != value)
                {
                    _modpackPath = value;
                    OnPropertyChanged(nameof(ModpackPath));

                    ServerProperties.ModpackPath = value;
                    JavaArguments.ModpackPath = value;
                }
            }
        }

        public ServerPropertiesViewModel ServerProperties { get; }
        public JavaArgumentsViewModel JavaArguments { get; }

        public MainWindowViewModel()
        {
            ServerProperties = new ServerPropertiesViewModel();
            JavaArguments = new JavaArgumentsViewModel();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
