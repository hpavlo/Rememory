namespace Rememory.Models.Metadata
{
    public class LinkMetadataModel : IMetadata
    {
        public MetadataFormat Format => MetadataFormat.Link;

        public string? Url { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Image { get; set; }

    }
}
