using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Windows.ApplicationModel;

namespace Rememory.Views.Settings
{
    public sealed partial class AboutPage : Page
    {
        private string _appName = AppInfo.Current.DisplayInfo.DisplayName;
        private string _appVersion = "AppVersion".GetLocalizedFormatResource(Package.Current.Id.Version.ToFormattedString());
        private string _githubLink = "https://github.com/hpavlo/Rememory";

        public AboutPage()
        {
            this.InitializeComponent();
        }
    }
}
