using Rememory.Helper;
using Rememory.Models;
using Rememory.Services;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Rememory.Contracts
{
    public interface IClipboardService
    {
        /// <summary>
        /// Occurs when clipboard was updated and we recived a new item
        /// </summary>
        event EventHandler<ClipboardEventArgs> NewItemAdded;

        /// <summary>
        /// Occurs when some item was moved to the top
        /// </summary>
        event EventHandler<ClipboardEventArgs> ItemMovedToTop;

        /// <summary>
        /// Occurs when we changed <seealso cref="ClipboardItem.IsFavorite"/> property
        /// </summary>
        event EventHandler<ClipboardEventArgs> FavoriteItemChanged;

        /// <summary>
        /// Occurs when one item was deleted
        /// </summary>
        event EventHandler<ClipboardEventArgs> ItemDeleted;

        /// <summary>
        /// Occurs when old items were deleted
        /// </summary>
        event EventHandler<ClipboardEventArgs> OldItemsDeleted;

        /// <summary>
        /// Occurs when all items were deleted
        /// </summary>
        event EventHandler<ClipboardEventArgs> AllItemsDeleted;

        /// <summary>
        /// Saves all items we have in DB
        /// </summary>
        List<ClipboardItem> ClipboardItems { get; }

        /// <summary>
        /// Start monitoring the system clipboard
        /// </summary>
        void StartClipboardMonitor();

        /// <summary>
        /// Stop monitoring the system clipboard
        /// </summary>
        void StopClipboardMonitor();

        /// <summary>
        /// Update current clipboard data with this item
        /// </summary>
        /// <param name="item">Item we want to set to clipboard</param>
        /// <param name="type">Set only specific type of the data</param>
        /// <returns>true if the clipboard was successfully updated</returns>
        bool SetClipboardData(ClipboardItem item, [Optional] ClipboardFormat? type);

        /// <summary>
        /// Adds new item to collection and DB
        /// </summary>
        /// <param name="item">New item we want to add to collection</param>
        void AddNewItem(ClipboardItem item);

        /// <summary>
        /// Remove item from the current position and insert it on 0 position
        /// </summary>
        /// <param name="item">Item we want to move</param>
        void MoveItemToTop(ClipboardItem item);

        /// <summary>
        /// Update <seealso cref="ClipboardItem.IsFavorite"/> field in item
        /// </summary>
        /// <param name="item">Item we want to update</param>
        void ChangeFavoriteItem(ClipboardItem item);

        /// <summary>
        /// Delete one item from collection and DB
        /// </summary>
        /// <param name="item">Item we want to delete</param>
        void DeleteItem(ClipboardItem item);

        /// <summary>
        /// Check and delete all older data for <paramref name="cutoffTime"/>
        /// </summary>
        /// <param name="cutoffTime">Time we will use to compare the data</param>
        /// <param name="deleteFavoriteItems">set false if we don't want to delete old favorite items</param>
        /// <returns>true if some items where delated</returns>
        bool DeleteOldItems(DateTime cutoffTime, bool deleteFavoriteItems);

        /// <summary>
        /// Erase all data from collection and DB
        /// </summary>
        void DeleteAllItems();
    }
}
