using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rememory.Services
{
    public partial class ClipboardService : IClipboardService
    {
        public event EventHandler<ClipboardEventArgs>? NewClipAdded;
        public event EventHandler<ClipboardEventArgs>? ClipMovedToTop;
        public event EventHandler<ClipboardEventArgs>? FavoriteClipChanged;
        public event EventHandler<ClipboardEventArgs>? ClipDeleted;
        public event EventHandler<ClipboardEventArgs>? AllClipsDeleted;

        public IList<ClipModel> Clips { get; private set; }

        private readonly IStorageService _storageService;
        private readonly IOwnerService _ownerService;
        private readonly ITagService _tagService;
        private readonly ILinkPreviewService _linkPreviewService;

        private readonly ClipboardMonitorCallback _clipboardCallback;
        private readonly Regex _hexColorRegex = HexColorRegex();
        private readonly Regex _hexColorRegexOptionalPrefix = HexColorRegexOptionalPrefix();

        public ClipboardService(IStorageService storageService, IOwnerService ownerService, ITagService tagService, ILinkPreviewService linkPreviewService)
        {
            _storageService = storageService;
            _ownerService = ownerService;
            _tagService = tagService;
            _linkPreviewService = linkPreviewService;
            _clipboardCallback = CallbackFunc;

            Clips = ReadClipsFromStorage();
        }

        public void StartClipboardMonitor(IntPtr windowHandle)
        {
            RememoryCoreHelper.StartClipboardMonitor(windowHandle, _clipboardCallback);
        }

        public void StopClipboardMonitor(IntPtr windowHandle)
        {
            RememoryCoreHelper.StopClipboardMonitor(windowHandle);
        }

        public bool SetClipboardData(ClipModel clip, ClipboardFormat? format = null, TextCaseType? caseType = null)
        {
            // Determine the list of formats to process
            List<ClipboardFormat> selectedFormats = format.HasValue ? [format.Value] : [.. clip.Data.Keys];

            string tempBitmapPath = string.Empty;
            bool generateBitmapFromPng = false;

            // Handle special case: if PNG is requested, also add Bitmap format and generate the bitmap file
            if (selectedFormats.Contains(ClipboardFormat.Png))
            {
                generateBitmapFromPng = ClipboardFormatHelper.TryGenerateBitmapFromPng(clip, out tempBitmapPath);
                if (generateBitmapFromPng && !selectedFormats.Contains(ClipboardFormat.Bitmap))
                {
                    // Insert Bitmap before Png if Png is present but Bitmap isn't explicitly requested
                    int pngIndex = selectedFormats.IndexOf(ClipboardFormat.Png);
                    if (pngIndex >= 0)
                    {
                        selectedFormats.Insert(pngIndex, ClipboardFormat.Bitmap);
                    }
                }
            }

            // Prepare the unmanaged structure for clipboard data info
            ClipboardDataInfo dataInfo = new()
            {
                FormatCount = (uint)selectedFormats.Count,
                FirstItem = Marshal.AllocHGlobal(selectedFormats.Count * Marshal.SizeOf(typeof(FormatDataItem)))
            };

            IntPtr bitmapUnmanagedPathPointer = IntPtr.Zero;
            nint currentPtr = dataInfo.FirstItem;

            // Process each selected format, convert data, and populate the unmanaged structure
            foreach (var currentFormat in selectedFormats)
            {
                IntPtr dataPtr = IntPtr.Zero;

                try
                {
                    // Convert data for the current format to unmanaged memory
                    if (currentFormat == ClipboardFormat.Bitmap && !string.IsNullOrEmpty(tempBitmapPath))
                    {
                        dataPtr = ClipboardFormatHelper.DataTypeToUnmanagedConverters[currentFormat](tempBitmapPath);
                    }
                    else if (clip.Data.TryGetValue(currentFormat, out var dataModel))
                    {
                        dataPtr = ClipboardFormatHelper.DataTypeToUnmanagedConverters[currentFormat](
                            currentFormat == ClipboardFormat.Text && caseType.HasValue
                            ? dataModel.Data.ConvertText(caseType.Value)
                            : dataModel.Data);
                    }

                    // Keep track of the unmanaged path for the bitmap
                    if (currentFormat == ClipboardFormat.Bitmap)
                    {
                        bitmapUnmanagedPathPointer = dataPtr;
                    }

                    Marshal.StructureToPtr(new FormatDataItem
                    {
                        Format = ClipboardFormatHelper.DataTypeFormats[currentFormat],
                        Data = dataPtr
                    }, currentPtr, false);
                    // Move the pointer to the next position in the unmanaged memory block
                    currentPtr = nint.Add(currentPtr, Marshal.SizeOf<FormatDataItem>());
                }
                catch { }
            }

            bool result = RememoryCoreHelper.SetDataToClipboard(ref dataInfo);

            // Clean up allocated unmanaged memory
            if (dataInfo.FirstItem != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(dataInfo.FirstItem);
            }
            if (bitmapUnmanagedPathPointer != IntPtr.Zero)
            {
                unsafe
                {
                    Utf16StringMarshaller.Free((ushort*)bitmapUnmanagedPathPointer);
                }                
            }

            // Schedule asynchronous cleanup of the temporary bitmap file
            if (!string.IsNullOrEmpty(tempBitmapPath))
            {
                Task.Run(async () =>
                {
                    // Wait for a short period to ensure the native clipboard operation is complete
                    await Task.Delay(5_000);
                    try
                    {
                        File.Delete(tempBitmapPath);
                    }
                    catch { }
                });
            }

            return result;
        }

        public void AddClip(ClipModel clip)
        {
            Clips.Insert(0, clip);
            _storageService.AddClip(clip);

            if (clip.Data.TryGetValue(ClipboardFormat.Text, out var textData))
            {
                if ((SettingsContext.Instance.RequireHexColorPrefix ? _hexColorRegex : _hexColorRegexOptionalPrefix).IsMatch(textData.Data))
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

                    _linkPreviewService.TryAddLinkMetadata(clip, textData);
                }
            }

            OnNewClipAdded(Clips, clip);

            if (SettingsContext.Instance.CleanupTypeIndex == (int)CleanupType.Quantity)
            {
                DeleteOldClipsByQuantity(SettingsContext.Instance.CleanupQuantity);
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

        public void DeleteClip(ClipModel clip, bool deleteFromDb = true)
        {
            Clips.Remove(clip);
            foreach (var tag in clip.Tags)
            {
                tag.Clips.Remove(clip);
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
            _storageService.DeleteOldClipsByTime(cutoffTime, deleteFavoriteClips);

            var clipsToDelete = Clips
                .Where(clip => clip.ClipTime < cutoffTime && (deleteFavoriteClips || !clip.IsFavorite))
                .ToList();

            foreach (var clip in clipsToDelete)
            {
                DeleteClip(clip, false);
            }
        }

        public void DeleteOldClipsByQuantity(int quantity)
        {
            if (Clips.Count <= quantity)
            {
                return;
            }

            _storageService.DeleteOldClipsByQuantity(quantity);

            var clipsToDelete = Clips
                .OrderByDescending(clip => clip.ClipTime)
                .TakeLast(Clips.Count - quantity)
                .ToList();

            foreach (var clip in clipsToDelete)
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

                App.Current.Exit();
            }

            return [];
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

            if (!TryMoveDuplicateItem(clip))
            {
                AddClip(clip);
            }

            return true;
        }

        private bool TryMoveDuplicateItem(ClipModel newClip)
        {
            if (Clips.FirstOrDefault(clip => clip.EqualDataTo(newClip)) is ClipModel toMove)
            {
                Clips.Remove(toMove);
                Clips.Insert(0, toMove);

                toMove.ClipTime = newClip.ClipTime;

                if (toMove.Owner != newClip.Owner)
                {
                    _ownerService.UnregisterClipOwner(toMove);
                    toMove.Owner = newClip.Owner;   // newClip.Owner is already registered
                }

                _storageService.UpdateClip(toMove);
                newClip.Owner = null;
                newClip.ClearExternalDataFiles();
                OnClipMovedToTop(Clips, toMove);
                return true;
            }

            return false;
        }

        [GeneratedRegex(@"^#([a-fA-F0-9]{8}|[a-fA-F0-9]{6}|[a-fA-F0-9]{4}|[a-fA-F0-9]{3})$")]
        private static partial Regex HexColorRegex();

        [GeneratedRegex(@"^#?([a-fA-F0-9]{8}|[a-fA-F0-9]{6}|[a-fA-F0-9]{4}|[a-fA-F0-9]{3})$")]
        private static partial Regex HexColorRegexOptionalPrefix();
    }
}
