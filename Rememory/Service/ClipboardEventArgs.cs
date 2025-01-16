using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Rememory.Service
{
    public class ClipboardEventArgs : EventArgs
    {
        public List<ClipboardItem> ClipboardItems { get; set; }
        public ClipboardItem ChangedClipboardItem { get; set; }
        public List<ClipboardItem> ChangedClipboardItems { get; set; }

        public ClipboardEventArgs(List<ClipboardItem> clipboardItems, [Optional] ClipboardItem changedClipboardItem, [Optional] List<ClipboardItem> changedClipboardItems)
        {
            ClipboardItems = clipboardItems;
            ChangedClipboardItem = changedClipboardItem;
            ChangedClipboardItems = changedClipboardItems;
        }
    }
}
