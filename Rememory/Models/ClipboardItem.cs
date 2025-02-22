using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Rememory.Models
{
    public partial class ClipboardItem : ObservableObject, IDisposable
    {
        private bool _disposed = false;

        public int Id { get; set; } = -1;

        public Dictionary<ClipboardFormat, string> DataMap = [];

        public Dictionary<ClipboardFormat, byte[]> HashMap = [];

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        private bool _isOpenInEditor = false;
        // Don't saved in DB
        public bool IsOpenInEditor
        {
            get => _isOpenInEditor;
            set => SetProperty(ref _isOpenInEditor, value);
        }

        private DateTime _time;
        public DateTime Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        public string OwnerPath;

        private byte[] _ownerIconBitmap;

        public byte[] OwnerIconBitmap
        {
            get => _ownerIconBitmap;
            set => SetProperty(ref _ownerIconBitmap, value);
        }

        public ClipboardItem() { }

        public ClipboardItem(ClipboardItem clipboardItem)
        {
            Id = clipboardItem.Id;
            DataMap = clipboardItem.DataMap;
            HashMap = clipboardItem.HashMap;
            IsFavorite = clipboardItem.IsFavorite;
            IsOpenInEditor = clipboardItem.IsOpenInEditor;
            Time = clipboardItem.Time;
            OwnerPath = clipboardItem.OwnerPath;
            OwnerIconBitmap = clipboardItem.OwnerIconBitmap;
        }

        ~ClipboardItem()
        {
            Dispose(false);
        }

        public void ClearSavedData()
        {
            foreach (var item in DataMap)
            {
                if (item.Key != ClipboardFormat.Text)
                {
                    try
                    {
                        var fileInfo = new FileInfo(item.Value);
                        if (fileInfo.Exists)
                        {
                            fileInfo.Delete();
                        }
                    }
                    catch { }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                ClearSavedData();

                if (DataMap != null)
                {
                    DataMap.Clear();
                    DataMap = null;
                }
                if (HashMap != null)
                {
                    HashMap.Clear();
                    HashMap = null;
                }
            }

            _disposed = true;
        }

        public void UpdateProperty([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
        }
    }
}
