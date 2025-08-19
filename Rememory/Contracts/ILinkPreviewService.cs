using Rememory.Models;

namespace Rememory.Contracts
{
    public interface ILinkPreviewService
    {
        void TryLoadLinkMetadata(DataModel dataModel);
    }
}
