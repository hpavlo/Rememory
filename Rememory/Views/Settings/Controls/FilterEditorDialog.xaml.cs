using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Rememory.Views.Settings.Controls
{
    public sealed partial class FilterEditorDialog : UserControl
    {
        public string FilterName { get; set; } = string.Empty;
        public string FilterPattern { get; set; } = string.Empty;

        private ContentDialog? _dialog;

        public FilterEditorDialog()
        {
            InitializeComponent();
        }

        private void Dialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (Parent is ContentDialog dialog)
            {
                _dialog = dialog;
            }
        }

        private void Pattern_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dialog is null)
            {
                return;
            }

            _dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(FilterPattern);
        }

        private void PatternExamplesButton_Click(object sender, RoutedEventArgs e)
        {
            PatternTeachingTip.IsOpen = !PatternTeachingTip.IsOpen;
        }
    }
}
