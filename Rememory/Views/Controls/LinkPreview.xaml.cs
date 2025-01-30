using Microsoft.UI.Xaml.Controls;
using Rememory.Models;
using Rememory.Views.Controls.Behavior;
using System.Runtime.InteropServices;

namespace Rememory.Views.Controls
{
    public sealed partial class LinkPreview : UserControl
    {
        public ClipboardLinkItem ItemContext => (ClipboardLinkItem)DataContext;

        public LinkPreview(ClipboardItem clipboardItem, [Optional] string searchText)
        {
            DataContext = clipboardItem;
            this.InitializeComponent();

            if (searchText is not null)
            {
                PreviewTextBlock.SearchHighlight(searchText, ItemContext.LinkValue);
            }
        }
    }
}
