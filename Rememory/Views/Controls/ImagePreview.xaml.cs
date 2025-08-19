using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Models;

namespace Rememory.Views.Controls
{
    public sealed partial class ImagePreview : UserControl
    {
        public DataModel ClipData
        {
            get => (DataModel)GetValue(ClipDataProperty);
            set => SetValue(ClipDataProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty ClipDataProperty =
            DependencyProperty.Register(nameof(ClipData), typeof(DataModel), typeof(ImagePreview), new PropertyMetadata(null, OnClipDataChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ImagePreview), new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public ImagePreview()
        {
            InitializeComponent();
        }

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }
    }
}
