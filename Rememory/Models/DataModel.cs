using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Models.Metadata;
using RememoryCore;

namespace Rememory.Models
{
    public partial class DataModel(ClipboardFormat format, string data, byte[] hash) : ObservableObject
    {
        public int Id { get; set; }

        public ClipboardFormat Format { get; set; } = format;

        public string Data { get; set; } = data;

        public byte[] Hash { get; set; } = hash;

        public IMetadata? Metadata
        {
            get;
            set => SetProperty(ref field, value);
        }
    }
}
