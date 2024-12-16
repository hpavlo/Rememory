using Rememory.Models;

namespace Rememory.Service
{
    public interface ILinkPreviewService
    {
        bool TryCreateLinkItem(ClipboardItem item, out ClipboardLinkItem linkItem);
    }
}
