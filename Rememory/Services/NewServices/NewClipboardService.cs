using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.NewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Rememory.Services.NewServices
{
    public class NewClipboardService
    {
        public IList<ClipModel> Clips { get; private set; }

        private readonly NewSqliteService _sqliteService = new();
        private readonly OwnerService _ownerService = new();
        private readonly LinkPreviewService _linkPreviewService = new();

        public NewClipboardService()
        {
            Clips = [.. _sqliteService.GetClips(_ownerService.Owners.Values.ToDictionary(o => o.Id))];
        }

        public void AddClip(ClipModel clip)
        {
            Clips.Insert(0, clip);
            _sqliteService.AddClip(clip);
            // OnNewClipAdded
        }

        public void MoveClipToTop(ClipModel clip)
        {
            Clips.Remove(clip);
            Clips.Insert(0, clip);
            clip.ClipTime = DateTime.Now;
            _sqliteService.UpdateClip(clip);
            // OnClipMovedToTop
        }

        public void ChangeFavoriteClip(ClipModel clip)
        {
            clip.IsFavorite = !clip.IsFavorite;
            _sqliteService.UpdateClip(clip);
            // OnFavoriteClipChanged
        }

        public void DeleteClip(ClipModel clip)
        {
            Clips.Remove(clip);
            clip.ClearExternalDataFiles();
            _sqliteService.DeleteClip(clip.Id);
            _ownerService.UnregisterClipOwner(clip);
            // OnClipDeleted
        }

        public void DeleteOldClips(DateTime cutoffTime, bool deleteFavoriteClips)
        {
            var clipsToDelete = Clips
                .Where(clip => clip.ClipTime < cutoffTime && (deleteFavoriteClips || !clip.IsFavorite));

            foreach (var clip in clipsToDelete)
            {
                DeleteClip(clip);
            }
        }

        public void DeleteAllItems()
        {
            // Delete all clips with owners and related data
            Clips.Clear();
            _sqliteService.DeleteAllClips();
            _ownerService.UnregisterAllOwners();

            try
            {
                var rtfPath = Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, ClipboardFormatHelper.FORMAT_FOLDER_NAME_RTF);
                var htmlPath = Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, ClipboardFormatHelper.FORMAT_FOLDER_NAME_HTML);
                var pngPath = Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, ClipboardFormatHelper.FORMAT_FOLDER_NAME_PNG);

                DeleteFolder(rtfPath);
                DeleteFolder(htmlPath);
                DeleteFolder(pngPath);
            }
            catch { }

            // OnAllClipsDeleted

            void DeleteFolder(string path)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
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
                if (dataFormat.Value == ClipboardFormat.Text)
                {
                    _linkPreviewService.TryAddLinkMetadata(clipData);
                }
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

                _sqliteService.UpdateClip(toBeMoved);
                newClip.Owner = null;
                newClip.ClearExternalDataFiles();
                //OnClipMovedToTop
                return true;
            }

            return false;
        }
    }
}
