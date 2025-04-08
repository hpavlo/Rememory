using Rememory.Models;
using System;
using System.Collections.Generic;

namespace Rememory.Contracts
{
    /// <summary>
    /// Defines the contract for managing application owners associated with clipboard entries.
    /// Handles registration, unregistration, and provides access to the current set of owners.
    /// </summary>
    public interface IOwnerService
    {
        /// <summary>
        /// Occurs when a new owner (application path) is identified and added to the active collection
        /// for the first time, typically when processing its first associated clip.
        /// </summary>
        event EventHandler<OwnerModel> OwnerRegistered;

        /// <summary>
        /// Occurs when an owner is removed from the active collection,
        /// usually because its clip reference count dropped to zero.
        /// The event argument is the path of the unregistered owner.
        /// </summary>
        event EventHandler<string> OwnerUnregistered;

        /// <summary>
        /// Occurs when the in-memory collection of owners is cleared via <see cref="UnregisterAllOwners"/>.
        /// </summary>
        event EventHandler AllOwnersUnregistered;

        /// <summary>
        /// Gets the dictionary of currently active owners being tracked in memory.
        /// The key is the owner's path (e.g., application executable path), and the value is the <see cref="OwnerModel"/>.
        /// </summary>
        /// <remarks>
        /// This represents an in-memory cache and might not reflect owners persisted in storage
        /// if they currently have no associated clips being tracked.
        /// </remarks>
        Dictionary<string, OwnerModel> Owners { get; }

        /// <summary>
        /// Processes a clip to register or update its associated owner.
        /// If the owner (based on path) is not already known, it's added.
        /// If it exists, its details (Name, Icon) might be updated.
        /// Associates the clip with the owner and increments the owner's internal clip count.
        /// </summary>
        /// <param name="clip">The clip being processed. Its <c>Owner</c> property will be set.</param>
        /// <param name="path">The path associated with the owner (e.g., application path). Can be null, often defaulted to an empty string for unknown owners.</param>
        /// <param name="icon">The icon associated with the owner. Can be null.</param>
        void RegisterClipOwner(ClipModel clip, string? path, byte[]? icon);

        /// <summary>
        /// Decrements the internal clip count for the owner associated with the provided clip.
        /// If the count reaches zero, the owner may be removed from the active collection
        /// (<see cref="Owners"/> dictionary) and potentially from persistent storage.
        /// Sets the clip's <c>Owner</c> property to null.
        /// </summary>
        /// <param name="clip">The clip whose owner should be unregistered or have its count decremented.</param>
        void UnregisterClipOwner(ClipModel clip);

        /// <summary>
        /// Clears the in-memory dictionary (<see cref="Owners"/>) of all tracked owners.
        /// </summary>
        /// <remarks>
        /// Caution: Еhis only clear the in-memory collection
        /// and not remove the corresponding owner records from persistent storage.
        /// </remarks>
        void UnregisterAllOwners();
    }
}
