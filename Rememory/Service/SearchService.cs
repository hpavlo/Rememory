using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Rememory.Service
{
    public class SearchService : ISearchService
    {
        private Task _searchTask;
        private CancellationTokenSource _cancellationTokenSource;

        public void StartSearching(IEnumerable<ClipboardItem> items,
                                   string searchString,
                                   Action<ClipboardItem> findedItemAction)
        {
            StopSearching();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _searchTask = Task.Run(() => SearchingAsync(items, searchString, findedItemAction, cancellationToken), cancellationToken);
        }

        public void StopSearching()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void SearchingAsync(IEnumerable<ClipboardItem> items,
                                    string searchString,
                                    Action<ClipboardItem> findedItemAction,
                                    CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (item.DataMap.TryGetValue(Helper.ClipboardFormat.Text, out string textData) &&
                    textData.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    App.Current.DispatcherQueue.TryEnqueue(() => findedItemAction.Invoke(item));
                }
            }
        }
    }
}
