using Rememory.Contracts;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Services
{
    public class SearchService : ISearchService
    {
        private Task _searchTask;
        private CancellationTokenSource _cancellationTokenSource;

        public void StartSearching(IEnumerable<ClipboardItem> items,
                                   string searchString,
                                   ObservableCollection<ClipboardItem> foundItems)
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

        private async void SearchingAsync(IEnumerable<ClipboardItem> items,
                                    string searchString,
                                    ObservableCollection<ClipboardItem> foundItems,
                                    CancellationToken cancellationToken)
        {
            App.Current.DispatcherQueue.TryEnqueue(foundItems.Clear);
            await Task.Delay(50);
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (item.DataMap.TryGetValue(Helper.ClipboardFormat.Text, out string textData) &&
                    textData.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    App.Current.DispatcherQueue.TryEnqueue(() => foundItems.Add(item));
                }
            }
        }
    }
}
