using Microsoft.UI.Xaml.Controls;

namespace Rememory.Views.Settings.Controls
{
    public sealed partial class FilterEditorDialogContent : UserControl
    {
        public string FilterName { get; set; } = string.Empty;
        public string FilterPattern { get; set; } = string.Empty;

        private readonly ContentDialog _dialog;

        public FilterEditorDialogContent(ContentDialog dialog)
        {
            this.InitializeComponent();
            _dialog = dialog;
        }

        private void Pattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            _dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(FilterPattern);
        }

        private void PatternExamplesButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            PatternTeachingTip.IsOpen = !PatternTeachingTip.IsOpen;
        }
    }
}
