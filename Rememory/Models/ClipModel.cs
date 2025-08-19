using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Rememory.Models
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

        public Dictionary<ClipboardFormat, DataModel> Data { get; set; } = [];

        public ObservableCollection<TagModel> Tags { get; set; } = [];

        #endregion

        private bool _isOpenInEditor = false;
        /// <summary>
        /// <c>True</c> if this clip is currently opened in editor 
        /// </summary>
        public bool IsOpenInEditor
        {
            get => _isOpenInEditor;
            set => SetProperty(ref _isOpenInEditor, value);
        }

        /// <summary>
        /// Used to show this clip in Links tab
        /// </summary>
        public bool IsLink { get; set; } = false;

        /// <summary>
        /// Using to hide tag list if the clip has no tags
        /// </summary>
        public bool HasTags => Tags.Any();

        public void TogglePropertyUpdate([CallerMemberName] string propertyName = "")
        {
            OnPropertyChanged(propertyName);
        }
    }
}
