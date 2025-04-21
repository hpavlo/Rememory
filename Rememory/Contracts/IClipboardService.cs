using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;

namespace Rememory.Contracts
{
    /// <summary>
    /// Defines the contract for the core clipboard management service.
    /// Handles monitoring, processing new clips, managing the clip collection,
    /// interacting with storage, and setting data back to the system clipboard.
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Occurs when a new clip, processed from a system clipboard update, has been added to the collection.
        /// </summary>
        event EventHandler<ClipboardEventArgs> NewClipAdded;

        /// <summary>
        /// Occurs when an existing clip is moved to the top of the collection,
        /// usually due to being re-copied or explicitly moved by user action.
        /// </summary>
        event EventHandler<ClipboardEventArgs> ClipMovedToTop;

        /// <summary>
        /// Occurs when the <see cref="ClipModel.IsFavorite"/> status of a clip has been changed.
        /// </summary>
        event EventHandler<ClipboardEventArgs> FavoriteClipChanged;

        /// <summary>
        /// Occurs when a clip has been deleted from the collection.
        /// </summary>
        event EventHandler<ClipboardEventArgs> ClipDeleted;

        /// <summary>
        /// Occurs when all clips have been deleted from the collection.
        /// </summary>
        event EventHandler<ClipboardEventArgs> AllClipsDeleted;

        /// <summary>
        /// Gets the in-memory list of tracked clips (<see cref="ClipModel"/>).
        /// This list represents the current working collection displayed to the user.
        /// </summary>
        IList<ClipModel> Clips { get; }

        /// <summary>
        /// Starts monitoring the system clipboard for changes using the specified window handle.
        /// </summary>
        /// <param name="windowHandle">The handle of the application window to associate with the clipboard monitoring chain.</param>
        void StartClipboardMonitor(IntPtr windowHandle);

        /// <summary>
        /// Stops monitoring the system clipboard associated with the specified window handle.
        /// </summary>
        /// <param name="windowHandle">The handle of the application window previously used to start monitoring.</param>
        void StopClipboardMonitor(IntPtr windowHandle);

        /// <summary>
        /// Sets the data from the specified clip model back onto the system clipboard.
        /// </summary>
        /// <param name="clip">The clip model containing the data to set.</param>
        /// <param name="format">Optional. If specified, only this data format from the clip will be set; otherwise, all available formats from the clip are attempted.</param>
        /// <param name="caseType">Optional. If specified, converts the text data to the specific test case.</param>
        /// <returns><c>true</c> if the clipboard was successfully updated; otherwise, <c>false</c>.</returns>
        bool SetClipboardData(ClipModel clip, ClipboardFormat? format = null, TextCaseType? caseType = null);

        /// <summary>
        /// Adds a new clip model to the beginning of the collection and persists it to storage.
        /// </summary>
        /// <param name="clip">The new clip model to add.</param>
        void AddClip(ClipModel clip);

        /// <summary>
        /// Moves the specified existing clip to the beginning (top) of the collection
        /// and updates its timestamp in persistent storage.
        /// </summary>
        /// <param name="clip">The clip model to move.</param>
        void MoveClipToTop(ClipModel clip);

        /// <summary>
        /// Toggles the <see cref="ClipModel.IsFavorite"/> status for the specified clip
        /// and updates the change in persistent storage.
        /// </summary>
        /// <param name="clip">The clip model whose favorite status should be changed.</param>
        void ChangeFavoriteClip(ClipModel clip);

        /// <summary>
        /// Deletes the specified clip from the in-memory collection and optionally from persistent storage.
        /// Also handles unregistering the associated owner if necessary.
        /// </summary>
        /// <param name="clip">The clip model to delete.</param>
        /// <param name="deleteFromDb">If <c>true</c>, also delete the clip from persistent storage; otherwise, only remove from the in-memory collection.</param>
        void DeleteClip(ClipModel clip, bool deleteFromDb = true);

        /// <summary>
        /// Deletes clips from the collection and persistent storage that are older than the specified cutoff time.
        /// </summary>
        /// <param name="cutoffTime">The date and time threshold. Clips older than this will be deleted.</param>
        /// <param name="deleteFavoriteClips">If <c>true</c>, favorite clips older than the cutoff time will also be deleted; otherwise, they will be preserved.</param>
        void DeleteOldClipsByTime(DateTime cutoffTime, bool deleteFavoriteClips);

        /// <summary>
        /// Deletes old clips from the collection and persistent storage if there are more than <paramref name="quantity"/>.
        /// </summary>
        /// <param name="quantity">Number of clips to leave.</param>
        void DeleteOldClipsByQuantity(int quantity);

        /// <summary>
        /// Deletes all clips from the collection and persistent storage.
        /// May also delete associated external files (e.g., images, RTF) and owner records depending on the implementation.
        /// </summary>
        void DeleteAllClips();
    }
}
