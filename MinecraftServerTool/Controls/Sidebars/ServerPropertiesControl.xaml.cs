using MinecraftServerTool.ViewModels;
using System.Windows.Controls;

namespace MinecraftServerTool.Controls
{
    public partial class ServerPropertiesControl : UserControl
    {
        public ServerPropertiesViewModel ViewModel { get; }

        public ServerPropertiesControl()
        {
            InitializeComponent();
        }
    }
}
