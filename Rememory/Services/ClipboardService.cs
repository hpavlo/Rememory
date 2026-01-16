using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Views.BriefMessage;
using RememoryCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace Rememory.Services
{
    public partial class ClipboardService : IClipboardService
    {
        private readonly ClipboardMonitor _clipboardMonitor = App.Current.Services.GetService<ClipboardMonitor>()!;

        public event EventHandler<ClipboardEventArgs>? NewClipAdded;
        public event EventHandler<ClipboardEventArgs>? ClipMovedToTop;
        public event EventHandler<ClipboardEventArgs>? FavoriteClipChanged;
        public event EventHandler<ClipboardEventArgs>? ClipDeleted;
        public event EventHandler<ClipboardEventArgs>? AllClipsDeleted;

        public SettingsContext SettingsContext => SettingsContext.Instance;

        public IList<ClipModel> Clips { get; private set; }

        private readonly IStorageService _storageService;
        private readonly IOwnerService _ownerService;
        private readonly ITagService _tagService;
        private readonly ILinkPreviewService _linkPreviewService;

        private readonly Regex _hexColorRegex = HexColorRegex();
        private readonly Regex _hexColorRegexOptionalPrefix = HexColorRegexOptionalPrefix();

        public ClipboardService(IStorageService storageService, IOwnerService ownerService, ITagService tagService, ILinkPreviewService linkPreviewService)
        {
            _storageService = storageService;
            _ownerService = ownerService;
            _tagService = tagService;
            _linkPreviewService = linkPreviewService;
            _clipboardMonitor.ContentDetected += ClipboardMonitor_ContentDetected;

            Clips = ReadClipsFromStorage();
        }

        public bool SetClipboardData(Dictionary<ClipboardFormat, DataModel> data, TextCaseType? caseType = null)
        {
            var dataMap = new Dictionary<ClipboardFormat, string>();

            foreach (var item in data)
            {
                string finalData = item.Value.Data;

                // Apply Text Case transformation if needed
                if (item.Key == ClipboardFormat.Text && caseType.HasValue)
                {
                    finalData = finalData.ConvertText(caseType.Value);
                }

                dataMap[item.Key] = finalData;
            }

            return _clipboardMonitor.SetClipboardData(dataMap);
        }

        public void AddClip(ClipModel clip)
        {
            Clips.Insert(0, clip);
            _storageService.AddClip(clip);

            if (clip.Data.TryGetValue(ClipboardFormat.Text, out var textData))
            {
                if ((SettingsContext.IsHexColorPrefixRequired ? _hexColorRegex : _hexColorRegexOptionalPrefix).IsMatch(textData.Data))
                {
                    ColorMetadataModel colorMetadata = new();
                    textData.Metadata = colorMetadata;
                    _storageService.AddColorMetadata(colorMetadata, textData.Id);
                }
                else
                {
                    // Detect if the new clip contains a link
                    clip.IsLink = Uri.TryCreate(textData.Data, UriKind.Absolute, out var uri)
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

                    if (SettingsContext.IsLinkPreviewLoadingEnabled && clip.IsLink)
                    {
                        _linkPreviewService.TryLoadLinkMetadata(textData);
                    }
                }
            }

            if (clip.Data.TryGetValue(ClipboardFormat.Files, out var filesData))
            {
                FilesMetadataModel filesMetadata = new(filesData.Data);
                filesData.Metadata = filesMetadata;
                _storageService.AddFilesMetadata(filesMetadata, filesData.Id);
            }

            OnNewClipAdded(Clips, clip);

            if (SettingsContext.CleanupType == CleanupType.Quantity)
            {
                DeleteOldClipsByQuantity(SettingsContext.CleanupQuantity, SettingsContext.IsFavoriteClipsCleaningEnabled);
            }
        }

        public void MoveClipToTop(ClipModel clip)
        {
            clip.ClipTime = DateTime.Now;
            _storageService.UpdateClip(clip);

            // Move the clip only if it's not on the top
            if (Clips.First() != clip)
            {
                Clips.Remove(clip);
                Clips.Insert(0, clip);
                OnClipMovedToTop(Clips, clip);
            }
        }

        public void ToggleClipFavorite(ClipModel clip)
        {
            clip.IsFavorite = !clip.IsFavorite;
            _storageService.UpdateClip(clip);
            OnFavoriteClipChanged(Clips, clip);
        }

        public async Task SaveClipToFileAsync(DataModel dataModel, string newFilePath)
        {
            try
            {
                var folderDestination = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(newFilePath));
                var newFile = await StorageFile.GetFileFromPathAsync(newFilePath);
                if (dataModel.IsFile() && File.Exists(dataModel.Data))
                {
                    var originFile = await StorageFile.GetFileFromPathAsync(dataModel.Data);
                    using var originStream = await originFile.OpenStreamForReadAsync();
                    using var destinationStream = await newFile.OpenStreamForWriteAsync();

                    await originStream.CopyToAsync(destinationStream);
                    await destinationStream.FlushAsync();
                }
                else if (dataModel.Format == ClipboardFormat.Text)
                {
                    var textFile = await StorageFile.GetFileFromPathAsync(newFilePath);
                    await FileIO.WriteTextAsync(textFile, dataModel.Data);
                }
            }
            catch { }
        }

        public void DeleteClip(ClipModel clip, bool deleteFromDb = true)
        {
            Clips.Remove(clip);
            foreach (var tag in clip.Tags)
            {
                tag.Clips.Remove(clip);
                tag.TogglePropertyUpdate(nameof(tag.ClipsCount));
            }
            clip.Tags.Clear();
            clip.ClearExternalDataFiles();
            if (deleteFromDb)
            {
                _storageService.DeleteClip(clip.Id);
            }
            _ownerService.UnregisterClipOwner(clip);
            OnClipDeleted(Clips, clip);
        }

        public void DeleteOldClipsByTime(DateTime cutoffTime, bool deleteFavoriteClips)
        {
            var clipsToDelete = Clips
                .Where(clip => clip.ClipTime < cutoffTime && (deleteFavoriteClips || !clip.IsFavorite) && clip.Tags.All(tag => tag.IsCleaningEnabled))
                .ToList();

            _storageService.DeleteOldClipsByTime(cutoffTime, deleteFavoriteClips);

            foreach (var clip in clipsToDelete)
            {
                DeleteClip(clip, false);
            }
        }

        public void DeleteOldClipsByQuantity(int quantity, bool deleteFavoriteClips)
        {
            var filteredClips = Clips
                .Where(clip => (deleteFavoriteClips || !clip.IsFavorite) && clip.Tags.All(tag => tag.IsCleaningEnabled))
                .OrderByDescending(clip => clip.ClipTime)
                .ToArray();

            if (filteredClips.Length <= quantity)
            {
                return;
            }

            _storageService.DeleteOldClipsByQuantity(quantity, deleteFavoriteClips);

            foreach (var clip in filteredClips.TakeLast(filteredClips.Length - quantity))
            {
                DeleteClip(clip, false);
            }
        }

        public void DeleteAllClips()
        {
            // Delete all clips with owners and related data
            foreach (var clip in Clips)
            {
                clip.Tags.Clear();
            }
            foreach (var tag in _tagService.Tags)
            {
                tag.Clips.Clear();
                tag.TogglePropertyUpdate(nameof(tag.ClipsCount));
            }
            Clips.Clear();
            _storageService.DeleteAllClips();
            _ownerService.UnregisterAllOwners();
            ClipboardFormatHelper.ClearAllExternalData();

            OnAllClipsDeleted(Clips);
        }

        protected virtual void OnNewClipAdded(IList<ClipModel> clips, ClipModel newClip)
        {
            NewClipAdded?.Invoke(this, new(clips, newClip));
        }
        protected virtual void OnClipMovedToTop(IList<ClipModel> clips, ClipModel newClip)
        {
            ClipMovedToTop?.Invoke(this, new(clips, newClip));
        }
        protected virtual void OnFavoriteClipChanged(IList<ClipModel> clips, ClipModel newClip)
        {
            FavoriteClipChanged?.Invoke(this, new(clips, newClip));
        }
        protected virtual void OnClipDeleted(IList<ClipModel> clips, ClipModel newClip)
        {
            ClipDeleted?.Invoke(this, new(clips, newClip));
        }
        protected virtual void OnAllClipsDeleted(IList<ClipModel> clips)
        {
            AllClipsDeleted?.Invoke(this, new(clips));
        }

        private IList<ClipModel> ReadClipsFromStorage()
        {
            try
            {
                return [.. _storageService.GetClips(_ownerService.Owners.Values.ToDictionary(o => o.Id), _tagService.Tags)];
            }
            catch
            {
                _ = NativeHelper.MessageBox(IntPtr.Zero,
                    "The data could not be retrieved from the database!\nIt may be corrupted. Try to reinstall the app",
                    "Rememory - Database error",
                    0x10);   // MB_ICONERROR | MB_OK

                Environment.Exit(1);
            }

            return [];
        }

        private void ClipboardMonitor_ContentDetected(ClipboardMonitor sender, ClipboardSnapshot snapshot)
        {
            string? ownerPath = snapshot.OwnerPath;
            byte[]? iconPixels = snapshot.OwnerIcon?.ToArray();

            if (!string.IsNullOrEmpty(ownerPath) && IsOwnerPathExcluded(ownerPath))
            {
                return;
            }

            ClipModel clip = new();

            foreach (var record in snapshot.Records)
            {
                if (!string.IsNullOrEmpty(record.Data) && record.Hash is not null && record.Hash.Length > 0)
                {
                    DataModel clipData = new(record.Format, record.Data, record.Hash.ToArray());
                    clip.Data.TryAdd(record.Format, clipData);
                }
            }

            if (clip.Data.Count == 0)
            {
                return;
            }

            App.Current.DispatcherQueue.TryEnqueue(() =>
            {
                _ownerService.RegisterClipOwner(clip, ownerPath, iconPixels);

                if (!TryMoveDuplicateItem(clip))
                {
                    AddClip(clip);
                }

                if (SettingsContext.IsClipCopyMessageEnabled)
                {
                    ShowToolTipMessage(clip);
                }
            });
        }

        private bool IsOwnerPathExcluded(string ownerPath)
        {
            var normalizedPath = ownerPath.Replace('\\', '/');
            try
            {
                var ownerFilter = SettingsContext.OwnerAppFilters.FirstOrDefault(filter =>
                    string.Equals(normalizedPath, filter.Pattern.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase) || filter.IsMatch(normalizedPath));

                if (ownerFilter is not null)
                {
                    ownerFilter.FilteredCount++;
                    SettingsContext.SaveOwnerAppFilters();
                    return true;
                }
            }
            catch { }
            return false;
        }

        private bool TryMoveDuplicateItem(ClipModel newClip)
        {
            if (Clips.FirstOrDefault(clip => clip.EqualDataTo(newClip)) is ClipModel toMove)
            {
                bool isMovedToTop = false;
                if (!toMove.Equals(Clips.FirstOrDefault()))
                {
                    Clips.Remove(toMove);
                    Clips.Insert(0, toMove);
                    isMovedToTop = true;
                    toMove.ClipTime = newClip.ClipTime;
                }

                if (toMove.Owner != newClip.Owner)
                {
                    _ownerService.UnregisterClipOwner(toMove);
                    toMove.Owner = newClip.Owner;   // newClip.Owner is already registered
                }

                _storageService.UpdateClip(toMove);
                newClip.Owner = null;
                newClip.ClearExternalDataFiles();

                if (isMovedToTop)
                {
                    OnClipMovedToTop(Clips, toMove);
                }

                return true;
            }

            return false;
        }

        private static void ShowToolTipMessage(ClipModel clip)
        {
            string iconGlyph = string.Empty;

            if (clip.Data.ContainsKey(ClipboardFormat.Text))
            {
                iconGlyph = "\uE8E9";
            }
            else if (clip.Data.ContainsKey(ClipboardFormat.Png) || clip.Data.ContainsKey(ClipboardFormat.Bitmap))
            {
                iconGlyph = "\uE91B";
            }
            else if (clip.Data.ContainsKey(ClipboardFormat.Files))
            {
                iconGlyph = "\uE8B7";
            }

            BriefMessageWindow.ShowBriefMessage(iconGlyph);
        }

        [GeneratedRegex(@"^#([a-fA-F0-9]{8}|[a-fA-F0-9]{6}|[a-fA-F0-9]{4}|[a-fA-F0-9]{3})$")]
        private static partial Regex HexColorRegex();

        [GeneratedRegex(@"^#?([a-fA-F0-9]{8}|[a-fA-F0-9]{6}|[a-fA-F0-9]{4}|[a-fA-F0-9]{3})$")]
        private static partial Regex HexColorRegexOptionalPrefix();
    }
}
