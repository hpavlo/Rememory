using Rememory.Models;
using System;
using System.Collections.Generic;

namespace Rememory.Service
{
    public interface ISearchService
    {
        void StartSearching(IEnumerable<ClipboardItem> items,
                            string searchString,
                            Action<ClipboardItem> findedItemAction);
        void StopSearching();
    }
}
