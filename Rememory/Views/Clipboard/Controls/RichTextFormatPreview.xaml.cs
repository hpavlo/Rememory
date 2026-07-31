using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Rememory.Models;
using System;
using System.IO;

namespace Rememory.Views.Clipboard.Controls
{
    public sealed partial class RichTextFormatPreview : DataPreviewBase
    {
        private const string RtfMarker = "{\\rtf1\\";
        private const string RtfLegacyMarker = "{\\rtf\\";

        public RichTextFormatPreview()
        {
            InitializeComponent();
        }

        protected override void OnClipDataChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnClipDataChanged(args);

            if (args.NewValue is DataModel clipData)
            {
                PreviewFormatedTextBox.IsReadOnly = false;
                string rtf = File.ReadAllText(clipData.Data);

                // Normalize RTF string before preview
                if (rtf.Length >= RtfLegacyMarker.Length && rtf.AsSpan().StartsWith(RtfLegacyMarker, StringComparison.Ordinal))
                {
                    rtf = string.Concat(RtfMarker, rtf.AsSpan(RtfLegacyMarker.Length));
                }

                PreviewFormatedTextBox.Document.SetText(TextSetOptions.FormatRtf, rtf);
                PreviewFormatedTextBox.IsReadOnly = true;
            }
            else
            {
                PreviewFormatedTextBox.IsReadOnly = false;
                PreviewFormatedTextBox.Document.SetText(TextSetOptions.FormatRtf, string.Empty);
                PreviewFormatedTextBox.IsReadOnly = true;
            }
        }

        protected override void OnSearchTextChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnSearchTextChanged(args);
            // Do not highlight rtf text to prevent clearing original text background
        }
    }
}
