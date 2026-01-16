using RememoryCore;
using System.IO;

namespace Rememory.Models.Metadata
{
    public class FilesMetadataModel : IMetadata
    {
        public MetadataFormat Format => MetadataFormat.Files;

        public int FilesCount { get; set; } = 0;

        public int FoldersCount { get; set; } = 0;

        public string[] Paths { get; private set; } = [];

        public FilesMetadataModel() { }

        public FilesMetadataModel(string filesPaths)
        {
            SetPaths(filesPaths);

            foreach (var path in Paths)
            {
                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                {
                    FoldersCount++;
                }
                else
                {
                    FilesCount++;
                }
            }
        }

        public void SetPaths(string filesPaths)
        {
            Paths = filesPaths.Split(FormatManager.FilePathsSeparator);
        }
    }
}
