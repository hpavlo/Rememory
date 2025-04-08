using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Rememory.Services
{
    public class ClipboardService : IClipboardService
    {
        public event EventHandler<ClipboardEventArgs>? NewClipAdded;
        public event EventHandler<ClipboardEventArgs>? ClipMovedToTop;
        public event EventHandler<ClipboardEventArgs>? FavoriteClipChanged;
        public event EventHandler<ClipboardEventArgs>? ClipDeleted;
        public event EventHandler<ClipboardEventArgs>? AllClipsDeleted;

        public IList<ClipModel> Clips { get; private set; }

        private readonly IStorageService _storageService;
        private readonly IOwnerService _ownerService;
        private readonly ILinkPreviewService _linkPreviewService;

        private readonly ClipboardMonitorCallback _clipboardCallback;

        public ClipboardService(IStorageService storageService, IOwnerService ownerService, ILinkPreviewService linkPreviewService)
        {
            _storageService = storageService;
            _ownerService = ownerService;
            _linkPreviewService = linkPreviewService;
            _clipboardCallback = CallbackFunc;

            Clips = [.. _storageService.GetClips(_ownerService.Owners.Values.ToDictionary(o => o.Id))];
        }

        public void StartClipboardMonitor(IntPtr windowHandle)
        {
            RememoryCoreHelper.StartClipboardMonitor(windowHandle, _clipboardCallback);
        }

        public void StopClipboardMonitor(IntPtr windowHandle)
        {
            RememoryCoreHelper.StopClipboardMonitor(windowHandle);
        }

        public unsafe bool SetClipboardData(ClipModel clip, ClipboardFormat? type = null)
        {
            List<ClipboardFormat> selectedTypes = type.HasValue ? [type.Value] : [.. clip.Data.Keys];

            ClipboardDataInfo dataInfo = new()
            {
                FormatCount = (uint)selectedTypes.Count,
                FirstItem = Marshal.AllocHGlobal(selectedTypes.Count * Marshal.SizeOf(typeof(FormatDataItem)))
            };

            nint currentPtr = dataInfo.FirstItem;
            foreach (var dataType in selectedTypes)
            {
                var dataStr = clip.Data.GetValueOrDefault(dataType);
                if (dataStr is null)
                {
                    continue;
                }

                var dataPtr = ClipboardFormatHelper.DataTypeToUnmanagedConverters[dataType](dataStr.Data);

                var formatItem = new FormatDataItem
                {
                    Format = ClipboardFormatHelper.DataTypeFormats[dataType],
                    Data = dataPtr
                };

                Marshal.StructureToPtr(formatItem, currentPtr, false);
                currentPtr = nint.Add(currentPtr, Marshal.SizeOf<FormatDataItem>());
            }

            var result = RememoryCoreHelper.SetDataToClipboard(ref dataInfo);
            Marshal.FreeHGlobal(dataInfo.FirstItem);
            return result;
        }

        public void AddClip(ClipModel clip)
        {
            if (clip.Data.TryGetValue(ClipboardFormat.Text, out var textData))
            {
                // Detect if the new clip contains a link
                clip.IsLink = Uri.TryCreate(textData.Data, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

                _linkPreviewService.TryAddLinkMetadata(clip, textData);
            }

            Clips.Insert(0, clip);
            _storageService.AddClip(clip);
            OnNewClipAdded(Clips, clip);
        }

        public void MoveClipToTop(ClipModel clip)
        {
            Clips.Remove(clip);
            Clips.Insert(0, clip);
            clip.ClipTime = DateTime.Now;
            _storageService.UpdateClip(clip);
            OnClipMovedToTop(Clips, clip);
        }

        public void ChangeFavoriteClip(ClipModel clip)
        {
            clip.IsFavorite = !clip.IsFavorite;
            _storageService.UpdateClip(clip);
            OnFavoriteClipChanged(Clips, clip);
        }

        public void DeleteClip(ClipModel clip, bool deleteFromDb = true)
        {
            Clips.Remove(clip);
            clip.ClearExternalDataFiles();
            if (deleteFromDb)
            {
                _storageService.DeleteClip(clip.Id);
            }
            _ownerService.UnregisterClipOwner(clip);
            OnClipDeleted(Clips, clip);
        }

        public void DeleteOldClips(DateTime cutoffTime, bool deleteFavoriteClips)
        {
            _storageService.DeleteOldClips(cutoffTime, deleteFavoriteClips);

            var clipsToDelete = Clips
                .Where(clip => clip.ClipTime < cutoffTime && (deleteFavoriteClips || !clip.IsFavorite))
                .ToList();

            foreach (var clip in clipsToDelete)
            {
                DeleteClip(clip, false);
            }
        }

        public void DeleteAllClips()
        {
            // Delete all clips with owners and related data
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

        private bool CallbackFunc(ref ClipboardDataInfo dataInfo)
        {
            string? ownerPath = Marshal.PtrToStringUni(dataInfo.OwnerPath);
            byte[]? iconPixels = null;
            if (!string.IsNullOrEmpty(ownerPath))
            {
                // Check should filter this source app or not
                var replacedOwnerPath = ownerPath.Replace('\\', '/');
                try
                {
                    var ownerFilter = SettingsContext.Instance.OwnerAppFilters.FirstOrDefault(filter => ownerPath.Equals(filter.Pattern)
                        || Regex.IsMatch(replacedOwnerPath, $"^{filter.Pattern.Replace('\\', '/').Replace("*", ".*")}$"));
                    if (ownerFilter is not null)
                    {
                        ownerFilter.FilteredCount++;
                        SettingsContext.Instance.OwnerAppFiltersSave();
                        return false;
                    }
                }
                catch { }

                if (dataInfo.IconPixels != 0)
                {
                    iconPixels = new byte[dataInfo.IconLength];
                    Marshal.Copy(dataInfo.IconPixels, iconPixels, 0, dataInfo.IconLength);
                }
            }

            ClipModel clip = new();
            _ownerService.RegisterClipOwner(clip, ownerPath, iconPixels);

            for (uint i = 0; i < dataInfo.FormatCount; i++)
            {
                var dataFormatInfo = Marshal.PtrToStructure<FormatDataItem>((nint)(dataInfo.FirstItem + i * Marshal.SizeOf<FormatDataItem>()));

                ClipboardFormat? dataFormat = ClipboardFormatHelper.GetFormatKeyByValue(dataFormatInfo.Format);
                if (dataFormat is null)
                {
                    continue;
                }

                string convertedData = ClipboardFormatHelper.DataTypeToStringConverters[dataFormat.Value]((dataFormatInfo.Data, dataFormatInfo.Size));
                if (string.IsNullOrEmpty(convertedData))
                {
                    return false;
                }

                var hash = new byte[32];
                Marshal.Copy(dataFormatInfo.Hash, hash, 0, 32);

                DataModel clipData = new(dataFormat.Value, convertedData, hash);
                clip.Data.TryAdd(dataFormat.Value, clipData);
            }

            if (!RemoveDuplicateItem(clip))
            {
                AddClip(clip);
            }

            return true;
        }

        private bool RemoveDuplicateItem(ClipModel newClip)
        {
            ClipModel? toBeMoved = null;

            foreach (var clip in Clips)
            {
                if (clip.EqualDataTo(newClip))
                {
                    toBeMoved = clip;
                    break;
                }
            }

            if (toBeMoved is not null)
            {
                Clips.Remove(toBeMoved);
                Clips.Insert(0, toBeMoved);

                toBeMoved.ClipTime = newClip.ClipTime;

                if (toBeMoved.Owner != newClip.Owner)
                {
                    _ownerService.UnregisterClipOwner(toBeMoved);
                    toBeMoved.Owner = newClip.Owner;   // newClip.Owner is already registered
                }

                _storageService.UpdateClip(toBeMoved);
                newClip.Owner = null;
                newClip.ClearExternalDataFiles();
                OnClipMovedToTop(Clips, toBeMoved);
                return true;
            }

            return false;
        }
    }
}
