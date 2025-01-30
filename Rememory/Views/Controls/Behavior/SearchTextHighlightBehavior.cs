using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Rememory.Views.Controls.Behavior
{
    public static class SearchTextHighlightBehavior
    {
        private static UISettings _uiSettings = new();
        private static Color _highlightColor => _uiSettings.GetColorValue(UIColorType.AccentLight3);

        /// <summary>
        /// Highlight the specific text in the TextBlock
        /// Use only after text is set or set in manually
        /// </summary>
        /// <param name="fullText">Optional parameter to set full text value instead of getting text from textBlock</param>
        public static void SearchHighlight(this TextBlock textBlock, string searchText, [Optional] string fullText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            // Regex to find all matches
            var regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
            var matches = regex.Matches(fullText ?? textBlock.Text);

            var highlighter = new TextHighlighter();
            highlighter.Background = new SolidColorBrush(_highlightColor);

            foreach (Match match in matches)
            {
                highlighter.Ranges.Add(new(match.Index, match.Length));
            }

            textBlock.TextHighlighters.Clear();
            textBlock.TextHighlighters.Add(highlighter);
        }

        /// <summary>
        /// Highlight the specific text in the RichEditBox
        /// Use only after text is set
        /// </summary>
        public static void SearchHighligh(this RichEditBox richEditBox, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            richEditBox.Document.GetText(TextGetOptions.None, out string text);

            // Regex to find all matches
            var regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                // Get the text range for the match
                ITextRange range = richEditBox.Document.GetRange(match.Index, match.Index + match.Length);
                range.CharacterFormat.BackgroundColor = _highlightColor;
            }
        }
    }
}
