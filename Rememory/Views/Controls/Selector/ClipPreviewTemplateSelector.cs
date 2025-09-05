using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;

namespace Rememory.Views.Controls.Selector
{
    public partial class ClipPreviewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TextTemplate { get; set; }
        public DataTemplate? LinkTemplate { get; set; }
        public DataTemplate? ImageTemplate { get; set; }
        public DataTemplate? FilesTemplate { get; set; }
        public DataTemplate? ColorTemplate { get; set; }
        public DataTemplate? EmptyTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ClipModel clipModel)
            {
                if (clipModel.IsLink)
                {
                    return LinkTemplate;
                }

                foreach (var dataItem in clipModel.Data)
                {
                    switch(dataItem.Key)
                    {
                        case ClipboardFormat.Text:
                            return dataItem.Value.Metadata is ColorMetadataModel ? ColorTemplate : TextTemplate;
                        case ClipboardFormat.Bitmap:
                        case ClipboardFormat.Png:
                            return ImageTemplate;
                        case ClipboardFormat.Files:
                            return FilesTemplate;
                    }
                }

                return EmptyTemplate;
            }

            return null;
        }
    }
}
