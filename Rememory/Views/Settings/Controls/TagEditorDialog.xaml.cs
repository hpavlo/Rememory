using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Rememory.Views.Settings.Controls
{
    public sealed partial class TagEditorDialog : UserControl
    {
        public string TagName { get; set; } = string.Empty;

        private ContentDialog? _dialog;

        public TagEditorDialog()
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

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dialog is null)
            {
                return;
            }

            _dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(TagName);
        }
    }
}
