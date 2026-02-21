using Rememory.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rememory.Contracts
{
    public interface IClipTransferService
    {
        /// <summary>
        /// Export selected clips with all related data to the external zip file
        /// </summary>
        /// <param name="clips">Clips to export</param>
        /// <param name="destinationFilePath">zip file path</param>
        Task<bool> ExportAsync(IList<ClipModel> clips, string destinationFilePath);

        /// <summary>
        /// Import all data from zip file
        /// </summary>
        /// <param name="filePath">Zip file to import</param>
        Task<bool> ImportAsync(string filePath);
    }
}
