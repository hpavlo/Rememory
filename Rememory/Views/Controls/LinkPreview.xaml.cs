using Microsoft.UI.Xaml.Controls;
using Rememory.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rememory.Views.Controls
{
    public sealed partial class LinkPreview : UserControl
    {
        public ClipboardLinkItem ItemContext => (ClipboardLinkItem)DataContext;   // Check if DataContext is ClipboardLinkItem

        public LinkPreview(ClipboardItem clipboardItem)
        {
            DataContext = clipboardItem;
            this.InitializeComponent();
        }
    }
}
