using Microsoft.UI.Xaml.Controls;
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
    }
}
