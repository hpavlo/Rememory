using Rememory.Models;
using Rememory.Models.Metadata;
using System;
using System.Collections.Generic;

namespace Rememory.Contracts
{
    /// <summary>
    /// Defines the contract for storage operations related to clipboard history,
    /// including owners (source applications), clips, and associated data/metadata.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Retrieves all owner records from the storage.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="OwnerModel"/>.</returns>
        IEnumerable<OwnerModel> GetOwners();

        /// <summary>
        /// Adds a new owner record to the storage. The Id property of the owner object might be updated after insertion.
        /// </summary>
        /// <param name="owner">The <see cref="OwnerModel"/> object to add.</param>
        void AddOwner(OwnerModel owner);

        /// <summary>
        /// Updates an existing owner record in the storage based on its Id.
        /// </summary>
        /// <param name="owner">The <see cref="OwnerModel"/> object with updated information.</param>
        void UpdateOwner(OwnerModel owner);

        /// <summary>
        /// Deletes an owner record from the storage using its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the owner to delete.</param>
        void DeleteOwner(int id);

        /// <summary>
        /// Retrieves all clip records from the storage.
        /// </summary>
        /// <param name="owners">A dictionary of pre-loaded owners (Id -> OwnerModel) used to efficiently associate clips with their owners during retrieval.</param>
        /// <returns>An enumerable collection of <see cref="ClipModel"/>, including associated data and owner information.</returns>
        IEnumerable<ClipModel> GetClips(Dictionary<int, OwnerModel> owners);

        /// <summary>
        /// Adds a new clip record to the storage, including its associated data formats. The Id property of the clip object might be updated after insertion.
        /// </summary>
        /// <param name="clip">The <see cref="ClipModel"/> object to add.</param>
        void AddClip(ClipModel clip);

        /// <summary>
        /// Updates an existing clip record in the storage (e.g., IsFavorite status, associated OwnerId) based on its Id.
        /// </summary>
        /// <param name="clip">The <see cref="ClipModel"/> object with updated information.</param>
        void UpdateClip(ClipModel clip);

        /// <summary>
        /// Deletes a clip record from the storage using its unique identifier. Associated data might also be deleted depending on implementation or database constraints.
        /// </summary>
        /// <param name="id">The unique identifier of the clip to delete.</param>
        void DeleteClip(int id);

        /// <summary>
        /// Deletes clip records older than a specified cutoff time.
        /// </summary>
        /// <param name="cutoffTime">The timestamp threshold; clips recorded before this time will be deleted.</param>
        /// <param name="deleteFavoriteClips">If true, favorite clips older than the cutoff time will also be deleted; otherwise, they will be kept.</param>
        void DeleteOldClipsByTime(DateTime cutoffTime, bool deleteFavoriteClips);

        /// <summary>
        /// Deletes old clips if there are more than <paramref name="quantity"/>.
        /// </summary>
        /// <param name="quantity">Number of clips to leave.</param>
        void DeleteOldClipsByQuantity(int quantity);

        /// <summary>
        /// Deletes all clip records from the storage. This might also clear related data like owners depending on the implementation.
        /// </summary>
        void DeleteAllClips();

        /// <summary>
        /// Adds link-specific metadata (URL, Title, Description, Image) associated with a specific data item within a clip.
        /// </summary>
        /// <param name="linkMetadata">The <see cref="LinkMetadataModel"/> containing the metadata to add.</param>
        /// <param name="dataId">The unique identifier of the <see cref="DataModel"/> item to which this metadata belongs.</param>
        void AddLinkMetadata(LinkMetadataModel linkMetadata, int dataId);

        /// <summary>
        /// Adds color-specific metadata associated with a specific data item within a clip.
        /// </summary>
        /// <param name="colorMetadataModel">The <see cref="ColorMetadataModel"/> containing the metadata to add.</param>
        /// <param name="dataId">The unique identifier of the <see cref="DataModel"/> item to which this metadata belongs.</param>
        void AddColorMetadata(ColorMetadataModel colorMetadataModel, int dataId);
    }
}
