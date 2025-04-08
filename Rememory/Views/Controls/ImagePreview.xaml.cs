using Microsoft.UI.Xaml.Controls;
using Rememory.Models;

namespace Rememory.Views.Controls
{
    public sealed partial class ImagePreview : UserControl
    {
        public string ImageUrl { get; private set; }

        public ImagePreview(DataModel dataModel)
        {
            DataContext = dataModel;
            ImageUrl = dataModel.Data;
            this.InitializeComponent();
        }
    }
}
