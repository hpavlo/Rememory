using Rememory.Models;
using System.Collections.Generic;

namespace Rememory.Contracts
{
    public interface IStorageService
    {
        int SaveClipboardItem(ClipboardItem item);
        void SaveLinkPreviewInfo(ClipboardLinkItem item);
        void UpdateClipboardItem(ClipboardItem item);
        void DeleteClipboardItem(ClipboardItem item);
        void DeleteAllClipboardItems();
        List<ClipboardItem> LoadClipboardItems();
    }
}
