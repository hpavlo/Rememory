using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Views.Controls.Behavior;

namespace Rememory.Views.Controls
{
    public sealed partial class EmptyPreview : UserControl
    {
        public EmptyPreview()
        {
            InitializeComponent();
        }
        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            string visualState = App.Current.SettingsContext.IsCompactViewEnabled && !PreviewControlsHelper.IsOpenInToolTip(this) ? "CompactView" : "NormalView";
            VisualStateManager.GoToState(this, visualState, true);
        }
    }
}
