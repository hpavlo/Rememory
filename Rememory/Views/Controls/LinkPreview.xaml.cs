using Microsoft.UI.Xaml.Controls;
using Rememory.Models;

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
