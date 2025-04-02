using Rememory.Helper;

namespace Rememory.Models.NewModels
{
    public class LinkMetadataModel(ClipboardFormat format, string data, byte[] hash) : DataModel(format, data, hash)
    {
        public string? Url { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Image { get; set; }
    }
}
