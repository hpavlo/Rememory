using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rememory.Views.Controls
{
    public sealed partial class ImagePreview : UserControl
    {
        public ClipboardItem ItemContext => (ClipboardItem)DataContext;

        public string ImageUrl => ItemContext.DataMap[ClipboardFormat.Png];

        public ImagePreview(ClipboardItem clipboardItem)
        {
            DataContext = clipboardItem;
            this.InitializeComponent();
        }
    }
}
