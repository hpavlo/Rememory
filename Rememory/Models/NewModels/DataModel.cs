using Rememory.Helper;

namespace Rememory.Models.NewModels
{
    public class DataModel(ClipboardFormat format, string data, byte[] hash)
    {
        public int Id { get; set; }

        public ClipboardFormat Format { get; set; } = format;

        public string Data { get; set; } = data;

        public byte[] Hash { get; set; } = hash;
    }
}
