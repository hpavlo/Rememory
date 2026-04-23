using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using System;
using Windows.ApplicationModel;
using Windows.System;

namespace Rememory.Views.Settings
{
    public sealed partial class AboutPage : Page
    {
        public string AppName { get; } = AppInfo.Current.DisplayInfo.DisplayName;
        public string AppDeveloper { get; } = "/Settings/About_DevelopedBy".GetLocalizedFormatResource("Pavlo Huk");
        public string AppVersion { get; } = "AppVersion".GetLocalizedFormatResource(Package.Current.Id.Version.ToFormattedString());
        public string GithubLink { get; } = "https://github.com/hpavlo/Rememory";
        public string MicrosoftStoreReviewLink { get; } = "ms-windows-store://review/?ProductId=9NKGMCQGVPL1";

        public AboutPage()
        {
            InitializeComponent();
        }

        private async void ReviewButton_Loaded(object sender, RoutedEventArgs e)
        {
            var reviewButton = sender as HyperlinkButton;
            var queryResult = await Launcher.QueryUriSupportAsync(new(MicrosoftStoreReviewLink), LaunchQuerySupportType.Uri);

            if (queryResult != LaunchQuerySupportStatus.Available)
            {
                reviewButton?.Visibility = Visibility.Collapsed;
            }
        }

        private async void ReviewButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new(MicrosoftStoreReviewLink));
        }
    }
}
