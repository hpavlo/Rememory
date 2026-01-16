using Rememory.Contracts;
using Rememory.Models;
using RememoryCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Services
{
    public class SearchService : ISearchService
    {
        private Task? _searchTask;
        private CancellationTokenSource? _cancellationTokenSource;

        public void StartSearching(IEnumerable<ClipModel> items,
                                   string searchString,
                                   ObservableCollection<ClipModel> foundItems)
        {
            StopSearching();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _searchTask = Task.Run(() => SearchingAsync(items, searchString, foundItems, cancellationToken), cancellationToken);
        }

        public void StopSearching()
        {
            _cancellationTokenSource?.Cancel();
        }

        private static async Task SearchingAsync(IEnumerable<ClipModel> items,
                                    string searchString,
                                    ObservableCollection<ClipModel> foundItems,
                                    CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(300, cancellationToken);

                var matches = await Task.Run(() =>
                {
                    var results = new List<ClipModel>();
                    foreach (var item in items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if ((item.Data.TryGetValue(ClipboardFormat.Text, out var dataModel) ||
                             item.Data.TryGetValue(ClipboardFormat.Files, out dataModel)) &&
                            dataModel.Data.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(item);
                        }
                    }
                    return results;
                }, cancellationToken);

                App.Current.DispatcherQueue.TryEnqueue(() =>
                {
                    foundItems.Clear();
                    foreach (var match in matches)
                    {
                        foundItems.Add(match);
                    }
                });
            }
            catch (OperationCanceledException) { }
        }
    }
}
