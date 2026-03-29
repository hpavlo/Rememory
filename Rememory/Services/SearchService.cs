using Rememory.Contracts;
using Rememory.Models;
using RememoryCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Services
{
    public class SearchService : ISearchService
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private string? _lastSearchString;

        public void StartSearch(IEnumerable<ClipModel> items, string searchString, ObservableCollection<ClipModel> foundItems)
        {
            StopSearch();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Run(() => SearchAsync(items, searchString, foundItems, cancellationToken), cancellationToken);
        }

        public void StopSearch()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task SearchAsync(IEnumerable<ClipModel> items, string searchString, ObservableCollection<ClipModel> foundItems, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(300, cancellationToken);

                // used to search in already filtered items
                bool useLocalSearch = !string.IsNullOrWhiteSpace(_lastSearchString)
                    && searchString.Length > _lastSearchString.Length
                    && searchString.Contains(_lastSearchString, StringComparison.OrdinalIgnoreCase);

                var contextToSearch = useLocalSearch ? foundItems : items;

                var matches = new List<ClipModel>();
                var matchesIds = new HashSet<int>();

                foreach (var item in contextToSearch)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ((item.Data.TryGetValue(ClipboardFormat.Text, out var dataModel) || item.Data.TryGetValue(ClipboardFormat.Files, out dataModel))
                        && dataModel.Data.Contains(searchString, StringComparison.OrdinalIgnoreCase))
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
    }
}
