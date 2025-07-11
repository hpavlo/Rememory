using Microsoft.UI.Xaml.Media;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;

namespace Rememory.Services
{
    public class TagService : ITagService
    {
        public event EventHandler<TagModel>? TagRegistered;
        public event EventHandler<int>? TagUnregistered;

        public IList<TagModel> Tags { get; private set; }

        private readonly IStorageService _storageService;

        public TagService(IStorageService storageService)
        {
            _storageService = storageService;

            Tags = ReadTagsFromStorage();
        }

        public void RegisterTag(string name, SolidColorBrush colorBrush)
        {
            TagModel tag = new(name, colorBrush);
            Tags.Add(tag);
            _storageService.AddTag(tag);
            OnTagRegistered(tag);
        }

        public void UnregisterTag(TagModel tag)
        {
            foreach (var clip in tag.Clips)
            {
                clip.Tags.Remove(tag);
                clip.UpdateProperty(nameof(clip.HasTags));
            }
            tag.Clips.Clear();
            Tags.Remove(tag);
            _storageService.DeleteTag(tag.Id);
            OnTagUnregistered(tag.Id);
        }

        public void UpdateTag(TagModel tag)
        {
            _storageService.UpdateTag(tag);
        }

        public void AddClipToTag(TagModel tag, ClipModel clip)
        {
            if (!clip.Tags.Contains(tag))
            {
                clip.Tags.Add(tag);
                tag.Clips.Add(clip);
                clip.UpdateProperty(nameof(clip.HasTags));
                tag.UpdateProperty(nameof(tag.ClipsCount));
                _storageService.AddClipTag(clip.Id, tag.Id);
            }
        }

        public void RemoveClipFromTag(TagModel tag, ClipModel clip)
        {
            if (clip.Tags.Remove(tag) && tag.Clips.Remove(clip))
            {
                clip.UpdateProperty(nameof(clip.HasTags));
                tag.UpdateProperty(nameof(tag.ClipsCount));
                _storageService.DeleteClipTag(clip.Id, tag.Id);
            }
        }

        protected virtual void OnTagRegistered(TagModel tag)
        {
            TagRegistered?.Invoke(this, tag);
        }

        protected virtual void OnTagUnregistered(int tagId)
        {
            TagUnregistered?.Invoke(this, tagId);
        }

        private IList<TagModel> ReadTagsFromStorage()
        {
            try
            {
                return [.. _storageService.GetTags()];
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
    }
}
