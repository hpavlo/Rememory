using Rememory.Models;
using System;
using System.Collections.Generic;

namespace Rememory.Service
{
    public class ClipboardEventArgs : EventArgs
    {
        public List<ClipboardItem> ClipboardItems { get; set; }
        public ClipboardItem ChangedClipboardItem { get; set; }

        public ClipboardEventArgs(List<ClipboardItem> clipboardItems, ClipboardItem changedClipboardItem)
        {
            ClipboardItems = clipboardItems;
            ChangedClipboardItem = changedClipboardItem;
        }
    }
}
