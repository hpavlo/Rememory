using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Rememory.ViewModels.Settings;

namespace Rememory.Views.Settings
{
    public sealed partial class MetadataPage : Page
    {
        public readonly MetadataPageViewModel ViewModel = new();

        public MetadataPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Bindings.StopTracking();
        }
    }
}
