using Microsoft.UI.Xaml;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class EmptyPreview : DataPreviewBase
    {
        public EmptyPreview()
        {
            InitializeComponent();
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            TriggerVisualState(PreviewVisualState);
        }
    }
}
