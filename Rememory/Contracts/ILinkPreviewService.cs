using Rememory.Models;

namespace Rememory.Contracts
{
    public interface ILinkPreviewService
    {
        void TryAddLinkMetadata(ClipModel clip, DataModel dataModel);
    }
}
