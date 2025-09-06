using System.Windows;
using System.Windows.Controls;

namespace MinecraftServerTool.Controls
{
    public partial class TooltipButton : UserControl
    {
        public TooltipButton()
        {
            InitializeComponent();
        }
        public string TooltipText
        {
            get { return (string)GetValue(TooltipTextProperty); }
            set { SetValue(TooltipTextProperty, value); }
        }

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register(nameof(TooltipText), typeof(string), typeof(TooltipButton),
                new PropertyMetadata(string.Empty, OnTooltipTextChanged));

        private static void OnTooltipTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TooltipButton tb)
            {
                tb.PART_TooltipText.Text = e.NewValue?.ToString();
            }
        }
    }
}
