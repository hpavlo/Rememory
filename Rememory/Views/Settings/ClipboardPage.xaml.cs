using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Rememory.ViewModels.Settings;

namespace Rememory.Views.Settings
{
    public sealed partial class ClipboardPage : Page
    {
        public readonly ClipboardPageViewModel ViewModel = new();

        public ClipboardPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Bindings.StopTracking();
            DataContext = null;
        }
    }
}
