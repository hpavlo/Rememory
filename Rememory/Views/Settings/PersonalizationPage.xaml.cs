using Microsoft.UI.Xaml.Controls;
using Rememory.ViewModels;

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

        public PersonalizationPage()
        {
            this.InitializeComponent();
        }
    }
}
