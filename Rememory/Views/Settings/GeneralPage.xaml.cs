using Microsoft.UI.Xaml.Controls;
using Rememory.ViewModels.Settings;

namespace Rememory.Views.Settings
{
    public sealed partial class GeneralPage : Page
    {
        public readonly GeneralPageViewModel ViewModel = new();

        public GeneralPage()
        {
            InitializeComponent();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                LanguageTeachingTip.IsOpen = true;
            }
        }
    }
}
