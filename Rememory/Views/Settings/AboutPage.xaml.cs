using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Windows.ApplicationModel;

namespace Rememory.Views.Settings
{
    public sealed partial class AboutPage : Page
    {
        public string AppName = AppInfo.Current.DisplayInfo.DisplayName;
        public string AppVersion = "AppVersion".GetLocalizedFormatResource(Package.Current.Id.Version.ToFormattedString());
        public string GithubLink = "https://github.com/hpavlo/Rememory";

        public AboutPage()
        {
            this.InitializeComponent();
        }
    }
}
