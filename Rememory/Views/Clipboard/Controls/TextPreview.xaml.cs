using Microsoft.UI.Xaml;
using Rememory.Models;
using System;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class TextPreview : DataPreviewBase
    {
        private const int TextDataLengthLimit = 1_000;
        private const int TextDataExpandedLengthLimit = 100_000;

        public static readonly DependencyProperty DataPreviewProperty =
            DependencyProperty.Register(nameof(DataPreview), typeof(string), typeof(TextPreview), new PropertyMetadata(string.Empty));
        public string DataPreview
        {
            get => (string)GetValue(DataPreviewProperty);
            set => SetValue(DataPreviewProperty, value);
        }

        public TextPreview()
        {
            InitializeComponent();
        }

        protected override void OnClipDataChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnClipDataChanged(args);

            if (args.NewValue is DataModel clipData)
            {
                var lengthLimit = PreviewVisualState == DataPreviewVisualState.Expanded ? TextDataExpandedLengthLimit : TextDataLengthLimit;
                DataPreview = clipData.Data.AsSpan(0, Math.Min(clipData.Data.Length, lengthLimit)).Trim().ToString();
                SearchHighlight(PreviewTextBlock, SearchText);
            }
            else
            {
                DataPreview = string.Empty;
            }
        }

        protected override void OnSearchTextChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnSearchTextChanged(args);

            if (args.NewValue is string searchText)
            {
                SearchHighlight(PreviewTextBlock, searchText);
            }
        }

        private void ParentControl_Loaded(object sender, RoutedEventArgs e)
        {
            SearchHighlight(PreviewTextBlock, SearchText);
        }
    }
}
