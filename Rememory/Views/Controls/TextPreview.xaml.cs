using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Models;

namespace Rememory.Views.Controls
{
    public sealed partial class TextPreview : UserControl
    {
        public ClipboardItem ItemContext => (ClipboardItem)DataContext;

        public string TextData => ItemContext.DataMap[ClipboardFormat.Text].Trim();

        public TextPreview(ClipboardItem clipboardItem)
        {
            DataContext = clipboardItem;
            this.InitializeComponent();
        }
    }
}
