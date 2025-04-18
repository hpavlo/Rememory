namespace Rememory.Contracts
{
    /// <summary>
    /// Cleans the data if the retention period of items has expired
    /// </summary>
    public interface ICleanupDataService
    {
        /// <summary>
        /// Check clean time and delete old clips
        /// </summary>
        void CleanupByRetentionPeriod();
    }
}
