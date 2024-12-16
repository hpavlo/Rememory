using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;

namespace Rememory.Service
{
    public interface IClipboardService
    {
        event EventHandler<ClipboardEventArgs> NewItemAdded;
        event EventHandler<ClipboardEventArgs> ItemMovedToTop;
        event EventHandler<ClipboardEventArgs> FavoriteItemChanged;
        event EventHandler<ClipboardEventArgs> ItemDeleted;
        event EventHandler<ClipboardEventArgs> OldItemsDeleted;
        List<ClipboardItem> ClipboardItems { get; }
        void StartClipboardMonitor();
        void StopClipboardMonitor();
        bool SetClipboardData(ClipboardItem item, ClipboardFormat? type = null);
        void MoveItemToTop(ClipboardItem item);
        void ChangeFavoriteItem(ClipboardItem item);
        void DeleteItem(ClipboardItem item);
        void DeleteOldItems(DateTime cutoffTime);
        void DeleteAllItems();
    }
}
