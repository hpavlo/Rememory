using Microsoft.UI.Xaml.Controls;
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
    }
}
