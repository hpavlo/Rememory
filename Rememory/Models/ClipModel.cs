using CommunityToolkit.Mvvm.ComponentModel;
using RememoryCore;
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

        public DateTime ClipTime
        {
            get;
            set => SetProperty(ref field, value);
        } = DateTime.Now;

        public bool IsFavorite
        {
            get;
            set => SetProperty(ref field, value);
        } = false;

        public OwnerModel? Owner { get; set; }

        public Dictionary<ClipboardFormat, DataModel> Data { get; set; } = [];

        public ObservableCollection<TagModel> Tags { get; set; } = [];

        #endregion

        /// <summary>
        /// <c>True</c> if this clip is currently opened in editor 
        /// </summary>
        public bool IsOpenInEditor { get;  set => SetProperty(ref field, value); } = false;

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
