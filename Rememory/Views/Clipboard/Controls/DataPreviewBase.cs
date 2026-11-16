using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

            string fullText = textData ?? textBlock.Text;
            if (string.IsNullOrEmpty(fullText))
            {
                return;
            }

            var highlighter = new TextHighlighter
            {
                Background = new SolidColorBrush(HighlightColor)
            };

            var rawRanges = new List<(int Index, int Length)>();

            var settings = App.Current?.SettingsContext;
            bool matchCase = settings?.IsMatchCaseEnabled == true;
            bool matchWholeWord = settings?.IsMatchWholeWordEnabled == true;
            bool useRegex = settings?.IsUseRegexEnabled == true;
            bool isFuzzy = settings?.IsFuzzySearchEnabled == true;

            try
            {
                if (useRegex)
                {
                    var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                    string pattern = matchWholeWord ? $@"\b(?:{searchText})\b" : searchText;
                    var regex = new Regex(pattern, options);
                    foreach (Match match in regex.Matches(fullText))
                    {
                        if (match.Length > 0)
                        {
                            rawRanges.Add((match.Index, match.Length));
                        }
                    }
                }
                else if (matchWholeWord)
                {
                    var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                    if (isFuzzy)
                    {
                        var terms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var searchRanges = new List<(int Index, int Length)>();
                        int currentIdx = 0;
                        bool allMatched = true;

                        foreach (var term in terms)
                        {
                            string pattern = $@"\b{Regex.Escape(term)}\b";
                            var regex = new Regex(pattern, options);
                            var matches = regex.Matches(fullText);
                            Match? foundMatch = null;
                            foreach (Match m in matches)
                            {
                                if (m.Index >= currentIdx)
                                {
                                    foundMatch = m;
                                    break;
                                }
                            }

                            if (foundMatch != null)
                            {
                                searchRanges.Add((foundMatch.Index, foundMatch.Length));
                                currentIdx = foundMatch.Index + foundMatch.Length;
                            }
                            else
                            {
                                allMatched = false;
                                break;
                            }
                        }

                        if (allMatched)
                        {
                            rawRanges.AddRange(searchRanges);
                        }
                    }
                    else
                    {
                        string pattern = $@"\b{Regex.Escape(searchText)}\b";
                        var regex = new Regex(pattern, options);
                        foreach (Match match in regex.Matches(fullText))
                        {
                            if (match.Length > 0)
                            {
                                rawRanges.Add((match.Index, match.Length));
                            }
                        }
                    }
                }
                else if (isFuzzy)
                {
                    var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    var terms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var searchRanges = new List<(int Index, int Length)>();
                    int currentIdx = 0;
                    bool allMatched = true;

                    foreach (var term in terms)
                    {
                        int subIdx = fullText.IndexOf(term, currentIdx, comparison);
                        if (subIdx >= 0)
                        {
                            searchRanges.Add((subIdx, term.Length));
                            currentIdx = subIdx + term.Length;
                        }
                        else
                        {
                            allMatched = false;
                            break;
                        }
                    }

                    if (allMatched)
                    {
                        rawRanges.AddRange(searchRanges);
                    }
                }
                else
                {
                    var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                    var exactRegex = new Regex(Regex.Escape(searchText), options);
                    var exactMatches = exactRegex.Matches(fullText);

                    if (exactMatches.Count > 0)
                    {
                        foreach (Match match in exactMatches)
                        {
                            rawRanges.Add((match.Index, match.Length));
                        }
                    }
                    else
                    {
                        var terms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        if (terms.Length > 1)
                        {
                            foreach (var term in terms)
                            {
                                var termRegex = new Regex(Regex.Escape(term), options);
                                var termMatches = termRegex.Matches(fullText);
                                if (termMatches.Count > 0)
                                {
                                    foreach (Match match in termMatches)
                                    {
                                        rawRanges.Add((match.Index, match.Length));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern while typing - ignore error
            }

            if (rawRanges.Count > 0)
            {
                AddMergedRanges(highlighter, rawRanges);
                textBlock.TextHighlighters.Add(highlighter);
            }
        }

        private static void AddMergedRanges(TextHighlighter highlighter, List<(int Index, int Length)> ranges)
        {
            if (ranges.Count == 0) return;

            var sorted = ranges.OrderBy(r => r.Index).ToList();
            int curStart = sorted[0].Index;
            int curEnd = curStart + sorted[0].Length;

            for (int i = 1; i < sorted.Count; i++)
            {
                int nextStart = sorted[i].Index;
                int nextEnd = nextStart + sorted[i].Length;

                if (nextStart <= curEnd)
                {
                    curEnd = Math.Max(curEnd, nextEnd);
                }
                else
                {
                    highlighter.Ranges.Add(new(curStart, curEnd - curStart));
                    curStart = nextStart;
                    curEnd = nextEnd;
                }
            }
            highlighter.Ranges.Add(new(curStart, curEnd - curStart));
        }
    }
}
