using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Rememory.Models;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Rememory.Views.Clipboard.Controls
{
    public enum DataPreviewVisualState
    {
        Compact,
        Standard,
        Expanded
    }

    public partial class DataPreviewBase : UserControl
    {
        protected const string CompactVisualStateName = "CompactView";
        protected const string StandardVisualStateName = "StandardView";
        protected const string ExpandedVisualStateName = "ExpandedView";

        private static readonly UISettings UiSettings = new();
        private static Color HighlightColor => UiSettings.GetColorValue(UIColorType.AccentLight3);

        public static readonly DependencyProperty ClipDataProperty =
            DependencyProperty.Register(nameof(ClipData), typeof(DataModel), typeof(DataPreviewBase), new PropertyMetadata(null, OnClipDataChanged));
        public DataModel? ClipData
        {
            get => (DataModel)GetValue(ClipDataProperty);
            set => SetValue(ClipDataProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(DataPreviewBase), new PropertyMetadata(string.Empty, OnSearchTextChanged));
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty PreviewVisualStateProperty =
            DependencyProperty.Register(nameof(PreviewVisualState), typeof(DataPreviewVisualState), typeof(DataPreviewBase), new PropertyMetadata(DataPreviewVisualState.Standard, OnPreviewVisualStateChanged));
        public DataPreviewVisualState PreviewVisualState
        {
            get => (DataPreviewVisualState)GetValue(PreviewVisualStateProperty);
            set => SetValue(PreviewVisualStateProperty, value);
        }

        public static readonly DependencyProperty IsTextSelectionEnabledProperty =
            DependencyProperty.Register(nameof(IsTextSelectionEnabled), typeof(bool), typeof(DataPreviewBase), new PropertyMetadata(false));
        public bool IsTextSelectionEnabled
        {
            get => (bool)GetValue(IsTextSelectionEnabledProperty);
            set => SetValue(IsTextSelectionEnabledProperty, value);
        }

        private static void OnClipDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataPreviewBase dataPreview)
            {
                dataPreview.OnClipDataChanged(e);
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataPreviewBase dataPreview)
            {
                dataPreview.OnSearchTextChanged(e);
            }
        }

        private static void OnPreviewVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataPreviewBase dataPreview)
            {
                if (e.NewValue is DataPreviewVisualState.Compact && !App.Current.SettingsContext.IsCompactViewEnabled)
                {
                    dataPreview.PreviewVisualState = (DataPreviewVisualState)e.OldValue;
                    return;
                }

                dataPreview.OnPreviewVisualStateChanged(e);
            }
        }

        protected virtual void OnClipDataChanged(DependencyPropertyChangedEventArgs args) { }
        protected virtual void OnSearchTextChanged(DependencyPropertyChangedEventArgs args) { }
        protected virtual void OnPreviewVisualStateChanged(DependencyPropertyChangedEventArgs args)
        {
            var visualStateName = (DataPreviewVisualState)args.NewValue;
            TriggerVisualState(visualStateName);
        }

        protected virtual void TriggerVisualState(DataPreviewVisualState visualState)
        {
            var visualStateName = visualState switch
            {
                DataPreviewVisualState.Compact => CompactVisualStateName,
                DataPreviewVisualState.Standard => StandardVisualStateName,
                DataPreviewVisualState.Expanded => ExpandedVisualStateName,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(visualStateName))
            {
                VisualStateManager.GoToState(this, visualStateName, true);
            }
        }

        /// <summary>
        /// Highlight the specific text in the TextBlock
        /// Use only after text is set or set in manually
        /// </summary>
        /// <param name="textData">The data we are searching in. If it's null - the data comes from <paramref name="textBlock"/></param>
        protected static void SearchHighlight(TextBlock textBlock, string searchText, string? textData = null)
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
    }
}
