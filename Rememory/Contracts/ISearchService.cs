using Rememory.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rememory.Contracts
{
    public interface ISearchService
    {
        void StartSearch(IEnumerable<ClipModel> items, string searchString, ObservableCollection<ClipModel> foundItems);
        void StopSearch();
    }
}
