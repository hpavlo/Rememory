using Microsoft.UI.Xaml.Media;
using Rememory.Models;
using System.Collections.Generic;

namespace Rememory.Contracts
{
    /// <summary>
    /// Service for managing tags, including registration, deletion, and updating.
    /// Handles associations between tags and clips.
    /// </summary>
    public interface ITagService
    {
        /// <summary>
        /// List of all tags currently stored in memory.
        /// </summary>
        IList<TagModel> Tags { get; }

        /// <summary>
        /// Registers a new tag by adding it to the collection and storing it in the database.
        /// </summary>
        /// <param name="name">The name of the tag to register.</param>
        /// <param name="colorBrush">Tag color brush.</param>
        void RegisterTag(string name, SolidColorBrush colorBrush);

        /// <summary>
        /// Unregisters a tag by removing its association with clips and deleting it from storage.
        /// </summary>
        /// <param name="tag">The tag to unregister.</param>
        void UnregisterTag(TagModel tag);

        /// <summary>
        /// Updates an existing tag in the database.
        /// </summary>
        /// <param name="tag">The tag with updated properties.</param>
        void UpdateTag(TagModel tag);

        /// <summary>
        /// Associates a clip with a tag and updates both collections accordingly.
        /// </summary>
        /// <param name="tag">The tag to associate with the clip.</param>
        /// <param name="clip">The clip to associate with the tag.</param>
        void AddClipToTag(TagModel tag, ClipModel clip);

        /// <summary>
        /// Removes the association between a clip and a tag.
        /// </summary>
        /// <param name="tag">The tag to remove from the clip.</param>
        /// <param name="clip">The clip to remove the tag from.</param>
        void RemoveClipFromTag(TagModel tag, ClipModel clip);
    }
}
