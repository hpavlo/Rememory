using Rememory.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rememory.Service
{
    public interface ISearchService
    {
        void StartSearching(IEnumerable<ClipboardItem> items,
                            string searchString,
                            ObservableCollection<ClipboardItem> foundItems);
        void StopSearching();
    }
}
