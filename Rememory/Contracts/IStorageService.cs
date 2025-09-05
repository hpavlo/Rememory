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
        /// <param name="tags">A list of pre-loaded tags used to efficiently associate clips with their tags during retrieval.</param>
        /// <returns>An enumerable collection of <see cref="ClipModel"/>, including associated data, tags and owner information.</returns>
        IEnumerable<ClipModel> GetClips(Dictionary<int, OwnerModel> owners, IList<TagModel> tags);

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
        /// <param name="deleteFavoriteClips">If true, old favorite clips will also be deleted; otherwise, they will be kept.</param>
        void DeleteOldClipsByQuantity(int quantity, bool deleteFavoriteClips);

        /// <summary>
        /// Deletes all clip records from the storage. This might also clear related data like owners depending on the implementation.
        /// </summary>
        void DeleteAllClips();

        /// <summary>
        /// Retrieves all tags from the database.
        /// </summary>
        /// <returns>An enumerable collection of TagModel objects.</returns>
        IEnumerable<TagModel> GetTags();

        /// <summary>
        /// Adds a new tag to the database and assigns it an ID.
        /// </summary>
        /// <param name="tag">The tag to add.</param>
        void AddTag(TagModel tag);

        /// <summary>
        /// Updates an existing tag's name in the database.
        /// </summary>
        /// <param name="tag">The tag with updated properties.</param>
        void UpdateTag(TagModel tag);

        /// <summary>
        /// Deletes a tag from the database by its ID.
        /// </summary>
        /// <param name="id">The ID of the tag to delete.</param>
        void DeleteTag(int id);

        /// <summary>
        /// Associates a clip with a tag in the database.
        /// </summary>
        /// <param name="clipId">The clip's ID.</param>
        /// <param name="tagId">The tag's ID.</param>
        void AddClipTag(int clipId, int tagId);

        /// <summary>
        /// Removes the association between a clip and a tag from the database.
        /// </summary>
        /// <param name="clipId">The clip's ID.</param>
        /// <param name="tagId">The tag's ID.</param>
        void DeleteClipTag(int clipId, int tagId);

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

        /// <summary>
        /// Adds files-specific metadata (FilesCount, FoldersCount) associated with a specific data item within a clip.
        /// </summary>
        /// <param name="filesMetadata">The <see cref="FilesMetadataModel"/> containing the metadata to add.</param>
        /// <param name="dataId">The unique identifier of the <see cref="DataModel"/> item to which this metadata belongs.</param>
        void AddFilesMetadata(FilesMetadataModel filesMetadata, int dataId);
    }
}
