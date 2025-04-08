using System.ComponentModel;

namespace Rememory.Models.Metadata
{
    public interface IMetadata
    {
        MetadataFormat Format { get; }
    }

    public enum MetadataFormat
    {
        [Description("Link")]
        Link
    }
}
