using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Rememory.Services
{
    public class ClipboardService : IClipboardService
    {
        public event EventHandler<ClipboardEventArgs> NewItemAdded;
        public event EventHandler<ClipboardEventArgs> ItemMovedToTop;
        public event EventHandler<ClipboardEventArgs> FavoriteItemChanged;
        public event EventHandler<ClipboardEventArgs> ItemDeleted;
        public event EventHandler<ClipboardEventArgs> OldItemsDeleted;
        public event EventHandler<ClipboardEventArgs> AllItemsDeleted;

        public List<ClipboardItem> ClipboardItems { get; private set; }

        private ClipboardMonitorCallback _clipboardCallback;

        private SettingsContext SettingsContext => SettingsContext.Instance;

        private readonly IStorageService _storageService = App.Current.Services.GetService<IStorageService>();
        private readonly ILinkPreviewService _linkPreviewService = App.Current.Services.GetService<ILinkPreviewService>();
        private readonly IOwnerAppService _ownerAppService = App.Current.Services.GetService<IOwnerAppService>();

        public ClipboardService()
        {
            _clipboardCallback = CallbackFunc;

            // Load items from DB
            ClipboardItems = _storageService.LoadClipboardItems();

            // Save each owner app info
            ClipboardItems.ForEach(item => _ownerAppService.RegisterNewItem(item.OwnerPath, item.OwnerIconBitmap));
        }

        public void StartClipboardMonitor()
        {
            RememoryCoreHelper.StartClipboardMonitor(App.Current.ClipboardWindowHandle, _clipboardCallback);
        }

        public void StopClipboardMonitor()
        {
            RememoryCoreHelper.StopClipboardMonitor(App.Current.ClipboardWindowHandle);
        }

        public unsafe bool SetClipboardData(ClipboardItem item, [Optional] ClipboardFormat? type)
        {
            List<ClipboardFormat> selectedTypes = type.HasValue ? new() { type.Value } : new(item.DataMap.Keys);

            ClipboardDataInfo dataInfo = new();
            dataInfo.FormatCount = (uint)selectedTypes.Count;
            dataInfo.FirstItem = Marshal.AllocHGlobal(selectedTypes.Count * Marshal.SizeOf(typeof(FormatDataItem)));

            nint currentPtr = dataInfo.FirstItem;
            foreach (var dataType in selectedTypes)
            {
                var dataStr = item.DataMap.GetValueOrDefault(dataType);
                if (dataStr == null)
                {
                    continue;
                }

                var dataPtr = ClipboardFormatHelper.DataTypeToUnmanagedConverters[dataType](dataStr);

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

        public void AddNewItem(ClipboardItem item)
        {
            if (_linkPreviewService.TryCreateLinkItem(item, out ClipboardLinkItem newLinkItem))
            {
                item = newLinkItem;
            }
            ClipboardItems.Insert(0, item);
            item.Id = _storageService.SaveClipboardItem(item);
            _ownerAppService.RegisterNewItem(item.OwnerPath, item.OwnerIconBitmap);
            OnNewItemAdded(new ClipboardEventArgs(ClipboardItems, item));
        }

        public void MoveItemToTop(ClipboardItem item)
        {
            ClipboardItems.Remove(item);
            ClipboardItems.Insert(0, item);
            item.Time = DateTime.Now;
            _storageService.UpdateClipboardItem(item);
            OnItemMovedToTop(new ClipboardEventArgs(ClipboardItems, item));
        }

        public void ChangeFavoriteItem(ClipboardItem item)
        {
            item.IsFavorite = !item.IsFavorite;
            _storageService.UpdateClipboardItem(item);
            OnFavoriteItemChanged(new ClipboardEventArgs(ClipboardItems, item));
        }

        public void DeleteItem(ClipboardItem item)
        {
            ClipboardItems.Remove(item);
            item.ClearSavedData();
            _storageService.DeleteClipboardItem(item);
            _ownerAppService.UnregisterItem(item.OwnerPath);
            OnItemDeleted(new ClipboardEventArgs(ClipboardItems, item));
        }

        public bool DeleteOldItems(DateTime cutoffTime, bool deleteFavoriteItems)
        {
            var itemsToDelete = ClipboardItems
                .Where(item => item.Time < cutoffTime && (deleteFavoriteItems || !item.IsFavorite))
                .ToList();

            if (itemsToDelete.Count == 0)
            {
                return false;
            }
            itemsToDelete.ForEach(item =>
            {
                ClipboardItems.Remove(item);
                item.ClearSavedData();
                _storageService.DeleteClipboardItem(item);
                _ownerAppService.UnregisterItem(item.OwnerPath);
            });

            OnOldItemsDeleted(new ClipboardEventArgs(ClipboardItems, changedClipboardItems: itemsToDelete));
            return true;
        }

        public void DeleteAllItems()
        {
            void DeleteFolder(string path)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }

            _storageService.DeleteAllClipboardItems();
            _ownerAppService.UnregisterAllItems();
            ClipboardItems.Clear();

            try
            {
                var rtfPath = Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, ClipboardFormatHelper.RTF_FORMAT_FOLDER_NAME);
                var htmlPath = Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, ClipboardFormatHelper.HTML_FORMAT_FOLDER_NAME);
                var pngPath = Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, ClipboardFormatHelper.PNG_FORMAT_FOLDER_NAME);

                DeleteFolder(rtfPath);
                DeleteFolder(htmlPath);
                DeleteFolder(pngPath);
            }
            catch { }

            OnAllItemsDeleted(new ClipboardEventArgs(ClipboardItems, changedClipboardItems: []));
        }

        private bool CallbackFunc(ref ClipboardDataInfo dataInfo)
        {
            ClipboardItem newItem = new()
            {
                Time = DateTime.Now
            };

            string ownerPathStr = Marshal.PtrToStringUni(dataInfo.OwnerPath);
            if (!string.IsNullOrEmpty(ownerPathStr))
            {
                // Check should filter this source app or not
                var replacedOwnerPath = ownerPathStr.Replace('\\', '/');
                try
                {
                    var ownerFilter = SettingsContext.OwnerAppFilters.FirstOrDefault(filter => ownerPathStr.Equals(filter.Pattern)
                        || Regex.IsMatch(replacedOwnerPath, $"^{filter.Pattern.Replace('\\', '/').Replace("*", ".*")}$"));
                    if (ownerFilter is not null)
                    {
                        ownerFilter.FilteredCount++;
                        SettingsContext.OwnerAppFiltersSave();
                        return false;
                    }
                }
                catch { }

                newItem.OwnerPath = ownerPathStr;
            }

            if (dataInfo.IconPixels != 0)
            {
                newItem.OwnerIconBitmap = GetIconBitmap(dataInfo);
            }
            
            for (uint i = 0; i < dataInfo.FormatCount; i++)
            {
                var dataFormatInfo = Marshal.PtrToStructure<FormatDataItem>((nint)(dataInfo.FirstItem + i * Marshal.SizeOf<FormatDataItem>()));

                var dataType = ClipboardFormatHelper.GetFormatKeyByValue(dataFormatInfo.Format).Value;
                string str = ClipboardFormatHelper.DataTypeToStringConverters[dataType]((dataFormatInfo.Data, dataFormatInfo.Size));
                if (string.IsNullOrEmpty(str))
                {
                    return false;
                }
                newItem.DataMap.Add(dataType, str);

                var hash = new byte[32];
                Marshal.Copy(dataFormatInfo.Hash, hash, 0, 32);
                newItem.HashMap.Add(dataType, hash);
            }

            if (!RemoveDuplicateItem(newItem))
            {
                AddNewItem(newItem);
            }
            
            return true;
        }

        private bool RemoveDuplicateItem(ClipboardItem newItem)
        {
            ClipboardItem toBeMoved = null;

            foreach (var item in ClipboardItems)
            {
                if (ClipboardFormatHelper.AreItemsEqual(item, newItem))
                {
                    toBeMoved = item;
                    break;
                }
            }

            if (toBeMoved is not null)
            {
                ClipboardItems.Remove(toBeMoved);
                ClipboardItems.Insert(0, toBeMoved);
                toBeMoved.Time = newItem.Time;
                toBeMoved.OwnerPath = newItem.OwnerPath;
                toBeMoved.OwnerIconBitmap = newItem.OwnerIconBitmap;
                _storageService.UpdateClipboardItem(toBeMoved);
                newItem.ClearSavedData();
                OnItemMovedToTop(new ClipboardEventArgs(ClipboardItems, toBeMoved));
                return true;
            }

            return false;
        }

        private byte[] GetIconBitmap(ClipboardDataInfo dataInfo)
        {
            byte[] pixels = new byte[dataInfo.IconLength];
            Marshal.Copy(dataInfo.IconPixels, pixels, 0, dataInfo.IconLength);
            return pixels;
        }

        protected void OnNewItemAdded(ClipboardEventArgs e)
        {
            NewItemAdded?.Invoke(this, e);
        }
        protected void OnItemMovedToTop(ClipboardEventArgs e)
        {
            ItemMovedToTop?.Invoke(this, e);
        }
        protected void OnFavoriteItemChanged(ClipboardEventArgs e)
        {
            FavoriteItemChanged?.Invoke(this, e);
        }
        protected void OnItemDeleted(ClipboardEventArgs e)
        {
            ItemDeleted?.Invoke(this, e);
        }
        protected void OnOldItemsDeleted(ClipboardEventArgs e)
        {
            OldItemsDeleted?.Invoke(this, e);
        }
        protected void OnAllItemsDeleted(ClipboardEventArgs e)
        {
            AllItemsDeleted?.Invoke(this, e);
        }
    }
}
