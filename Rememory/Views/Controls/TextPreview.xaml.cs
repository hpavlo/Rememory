using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Views.Controls.Behavior;
using System.Runtime.InteropServices;

namespace Rememory.Views.Controls
{
    public sealed partial class TextPreview : UserControl
    {
        public ClipboardItem ItemContext => (ClipboardItem)DataContext;

        public string TextData => ItemContext.DataMap[ClipboardFormat.Text].Trim();

        public TextPreview(ClipboardItem clipboardItem, [Optional] string searchText)
        {
            DataContext = clipboardItem;
            this.InitializeComponent();

            if (searchText is not null)
            {
                PreviewTextBlock.SearchHighlight(searchText, TextData);
            }
        }
    }
}
