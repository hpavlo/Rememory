using System.ComponentModel;

namespace Rememory.Models.NewModels
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
