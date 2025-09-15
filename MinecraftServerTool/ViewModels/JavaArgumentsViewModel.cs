using MinecraftServerTool.Helpers;
using MinecraftServerTool.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MinecraftServerTool.ViewModels
{
    public class JavaArgumentsViewModel : INotifyPropertyChanged
    {
        // Internal variables for flow
        private int _allocatedRam = 4;
        public ICommand SaveCommand { get; }

        // External variables from viewmodels
        public string ModpackPath { get; set; }

        // POCOs
        public int AllocatedRam
        {
            get => _allocatedRam;
            set
            {
                if (_allocatedRam != value)
                {
                    _allocatedRam = value;
                    OnPropertyChanged();
                }
            }
        }


        private readonly JavaArgumentsService javaService;

        public JavaArgumentsViewModel()
        {
            javaService = new JavaArgumentsService();
            SaveCommand = new RelayCommand(_ => SaveArguments());
        }

        private void SaveArguments()
        {
            JavaArgumentsService.SaveJvmArgs(ModpackPath, AllocatedRam);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
