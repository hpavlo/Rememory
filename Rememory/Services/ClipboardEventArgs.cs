using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Rememory.Services
{
    public class ClipboardEventArgs : EventArgs
    {
        /// <summary>
        /// All clipboard items we have after update
        /// </summary>
        public List<ClipboardItem> ClipboardItems { get; set; }
        /// <summary>
        /// Only changed clipboard item
        /// </summary>
        public ClipboardItem ChangedClipboardItem { get; set; }
        /// <summary>
        /// Only changed clipboard items
        /// </summary>
        public List<ClipboardItem> ChangedClipboardItems { get; set; }

        public ClipboardEventArgs(List<ClipboardItem> clipboardItems, [Optional] ClipboardItem changedClipboardItem, [Optional] List<ClipboardItem> changedClipboardItems)
        {
            ClipboardItems = clipboardItems;
            ChangedClipboardItem = changedClipboardItem;
            ChangedClipboardItems = changedClipboardItems;
        }
    }
}
