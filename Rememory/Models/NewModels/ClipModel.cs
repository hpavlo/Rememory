using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Helper;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Rememory.Models.NewModels
{
    public partial class ClipModel : ObservableObject
    {
        #region DB columns

        public int Id { get; set; }

        private DateTime _clipTime = DateTime.Now;
        public DateTime ClipTime
        {
            get => _clipTime;
            set => SetProperty(ref _clipTime, value);
        }

        private bool _isFavorite = false;
        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public OwnerModel? Owner { get; set; }

        public Dictionary<ClipboardFormat, DataModel> Data = [];

        #endregion

        private bool _isOpenInEditor = false;
        public bool IsOpenInEditor
        {
            get => _isOpenInEditor;
            set => SetProperty(ref _isOpenInEditor, value);
        }

        public void UpdateProperty([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
        }
    }
}
