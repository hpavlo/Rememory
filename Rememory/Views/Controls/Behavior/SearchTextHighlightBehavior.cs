using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Rememory.Views.Controls.Behavior
{
    public static class SearchTextHighlightBehavior
    {
        private static readonly UISettings UiSettings = new();
        private static Color HighlightColor => UiSettings.GetColorValue(UIColorType.AccentLight3);

        /// <summary>
        /// Highlight the specific text in the TextBlock
        /// Use only after text is set or set in manually
        /// </summary>
        /// <param name="searchText">Search pattern</param>
        /// <param name="textData">The data we are searching in. If it's null - the data comes from <paramref name="textBlock"/></param>
        public static void SearchHighlight(this TextBlock textBlock, string searchText, string? textData = null)
        {
            textBlock.TextHighlighters.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            // Regex to find all matches
            var regex = new Regex(Regex.Escape(searchText), RegexOptions.IgnoreCase);
            var matches = regex.Matches(textData ?? textBlock.Text);

            var highlighter = new TextHighlighter();
            highlighter.Background = new SolidColorBrush(HighlightColor);

            foreach (Match match in matches)
            {
                highlighter.Ranges.Add(new(match.Index, match.Length));
            }

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
                range.CharacterFormat.BackgroundColor = HighlightColor;
            }
        }
    }
}
