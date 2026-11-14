using Rememory.Contracts;
using Rememory.Core;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Services
{
    public class SearchService : ISearchService
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _lastSearchString;

        public void StartSearch(
            IEnumerable<ClipModel> items,
            string searchString,
            ObservableCollection<ClipModel> foundItems,
            bool matchCase = false,
            bool matchWholeWord = false,
            bool useRegex = false,
            bool fuzzy = false)
        {
            StopSearch();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Run(() => SearchAsync(items, searchString, foundItems, matchCase, matchWholeWord, useRegex, fuzzy, cancellationToken), cancellationToken);
        }

        public void StopSearch()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task SearchAsync(
            IEnumerable<ClipModel> items,
            string searchString,
            ObservableCollection<ClipModel> foundItems,
            bool matchCase,
            bool matchWholeWord,
            bool useRegex,
            bool fuzzy,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(300, cancellationToken);

                // used to search in already filtered items
                bool useLocalSearch = !fuzzy
                    && !matchCase
                    && !matchWholeWord
                    && !useRegex
                    && !string.IsNullOrWhiteSpace(_lastSearchString)
                    && searchString.Length > _lastSearchString.Length
                    && searchString.Contains(_lastSearchString, StringComparison.OrdinalIgnoreCase);

                var contextToSearch = useLocalSearch ? foundItems : items;

                var matches = new List<ClipModel>();
                var matchesIds = new HashSet<int>();

                foreach (var item in contextToSearch)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    bool matched = false;
                    if (item.Data.TryGetValue(ClipboardFormat.Text, out var dataModel) || item.Data.TryGetValue(ClipboardFormat.Files, out dataModel))
                    {
                        matched = IsMatch(dataModel.Data, searchString, matchCase, matchWholeWord, useRegex, fuzzy);
                    }

                    if (matched)
                    {
                        if (useLocalSearch)
                        {
                            matchesIds.Add(item.Id);
                        }
                        else
                        {
                            matches.Add(item);
                        }
                    }
                }

                App.Current.DispatcherQueue.TryEnqueue(() =>
                {
                    if (useLocalSearch)
                    {
                        // remove items no longer matching
                        var itemsToRemove = foundItems.Where(oldItem => !matchesIds.Contains(oldItem.Id)).ToList();
                        foreach (var item in itemsToRemove)
                        {
                            foundItems.Remove(item);
                        }
                    }
                    else
                    {
                        foundItems.Clear();
                        foreach (var match in matches)
                        {
                            foundItems.Add(match);
                        }
                    }
                });

                _lastSearchString = searchString;
            }
            catch (OperationCanceledException) { }
        }

        private static bool IsMatch(string text, string searchString, bool matchCase, bool matchWholeWord, bool useRegex, bool fuzzy)
        {
            if (string.IsNullOrWhiteSpace(searchString)) return true;
            if (string.IsNullOrEmpty(text)) return false;

            if (useRegex)
            {
                try
                {
                    var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                    string pattern = matchWholeWord ? $@"\b(?:{searchString})\b" : searchString;
                    return Regex.IsMatch(text, pattern, options);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            if (matchWholeWord)
            {
                var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (fuzzy)
                {
                    var tokens = searchString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    int currentIdx = 0;
                    foreach (var token in tokens)
                    {
                        string pattern = $@"\b{Regex.Escape(token)}\b";
                        var matches = Regex.Matches(text, pattern, options);
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
                            currentIdx = foundMatch.Index + foundMatch.Length;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    string pattern = $@"\b{Regex.Escape(searchString)}\b";
                    return Regex.IsMatch(text, pattern, options);
                }
            }

            if (fuzzy)
            {
                var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                var tokens = searchString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int currentIdx = 0;

                foreach (var token in tokens)
                {
                    int subIdx = text.IndexOf(token, currentIdx, comparison);
                    if (subIdx < 0)
                    {
                        return false;
                    }
                    currentIdx = subIdx + token.Length;
                }

                return true;
            }

            var comp = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return text.Contains(searchString, comp);
        }
    }
}
