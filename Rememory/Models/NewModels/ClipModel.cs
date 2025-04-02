using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Helper;
using System;
using System.Collections.Generic;

namespace Rememory.Models.NewModels
{
    public partial class ClipModel : ObservableObject
    {
        public int Id { get; set; }

        private DateTime _clipTime;
        public DateTime ClipTime
        {
            get => _clipTime;
            set => SetProperty(ref _clipTime, value);
        }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public OwnerModel? Owner { get; set; }

        public Dictionary<ClipboardFormat, DataModel>? Data;
    }
}
