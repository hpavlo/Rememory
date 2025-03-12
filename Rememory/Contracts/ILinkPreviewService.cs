using Rememory.Models;

namespace Rememory.Contracts
{
    public interface ILinkPreviewService
    {
        bool TryCreateLinkItem(ClipboardItem item, out ClipboardLinkItem linkItem);
    }
}
