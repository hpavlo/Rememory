using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Rememory.ViewModels;
using System.Collections.Generic;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rememory.Views.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PersonalizationPage : Page
    {
        public readonly SettingsPersonalizationPageViewModel ViewModel = new();

        private readonly List<SolidColorBrush> _suggestedBackgroundColors = [
            new(Color.FromArgb(0, 0, 0, 0)),
            new(Color.FromArgb(50, 255, 185, 0)),
            new(Color.FromArgb(50, 247, 99, 12)),
            new(Color.FromArgb(50, 209, 52, 56)),
            new(Color.FromArgb(50, 234, 0, 94)),
            new(Color.FromArgb(50, 0, 120, 215)),
            new(Color.FromArgb(50, 135, 100, 184)),
            new(Color.FromArgb(50, 177, 70, 194)),
            new(Color.FromArgb(50, 0, 153, 188)),
            new(Color.FromArgb(50, 0, 183, 195)),
            new(Color.FromArgb(50, 0, 178, 148)),
            new(Color.FromArgb(50, 16, 124, 16)),
            new(Color.FromArgb(50, 76, 74, 72)),
            new(Color.FromArgb(50, 105, 121, 126))   // Default for windows 10
            ];

        public PersonalizationPage()
        {
            this.InitializeComponent();
        }

        private void BackgroundColorItemsViewSelectionUpdate()
        {
            var index = _suggestedBackgroundColors.FindIndex(item => item.Color == ViewModel.SettingsContext.CurrentWindowBackgroundBrush.Color);
            BackgroundColorItemsView.Select(index);
        }

        private void BackgroundColorFlyout_Closed(object sender, object e)
        {
            ViewModel.SettingsContext.CurrentWindowBackgroundBrush = new SolidColorBrush(BackgroundColorPicker.Color);
            BackgroundColorItemsViewSelectionUpdate();
        }

        private void BackgroundColorItemsView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            BackgroundColorItemsViewSelectionUpdate();
        }

        private void BackgroundColorPicker_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            BackgroundColorPicker.Color = ViewModel.SettingsContext.CurrentWindowBackgroundBrush.Color;
        }

        private void BackgroundColorItemsView_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
        {
            if (sender.SelectedItem is not null)
            {
                ViewModel.SettingsContext.CurrentWindowBackgroundBrush = (SolidColorBrush)sender.SelectedItem;
            }
        }
    }
}
