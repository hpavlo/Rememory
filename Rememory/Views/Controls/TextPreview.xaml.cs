using Microsoft.UI.Xaml.Controls;
using Rememory.Models;
using Rememory.Views.Controls.Behavior;
using System.Runtime.InteropServices;

namespace Rememory.Views.Controls
{
    public sealed partial class TextPreview : UserControl
    {
        public string TextData { get; private set; }

        public TextPreview(DataModel dataModel, [Optional] string? searchText)
        {
            DataContext = dataModel;
            TextData = dataModel.Data.Trim();
            this.InitializeComponent();

            if (searchText is not null)
            {
                PreviewTextBlock.SearchHighlight(searchText, TextData);
            }
        }
    }
}
