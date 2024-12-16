using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Windows.ApplicationModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rememory.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        private string _appName = "AppDisplayName".GetLocalizedResource();
        private string _appVersion = "AppVersion".GetLocalizedFormatResource(Package.Current.Id.Version.ToFormattedString());
        private string _githubLink = "https://github.com/hpavlo/Rememory";

        public AboutPage()
        {
            this.InitializeComponent();
        }
    }
}
