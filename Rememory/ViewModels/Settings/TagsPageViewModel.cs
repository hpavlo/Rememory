using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Models;
using System.Collections.ObjectModel;

namespace Rememory.ViewModels.Settings
{
    public partial class TagsPageViewModel : ObservableObject
    {
        private readonly ITagService _tagService = App.Current.Services.GetService<ITagService>()!;

        public SettingsContext SettingsContext => SettingsContext.Instance;

        public ObservableCollection<TagModel> Tags;

        public TagsPageViewModel()
        {
            Tags = [.. _tagService.Tags];
        }

        public void AddTag(string name, string colorHex, bool isCleaningEnabled)
        {
            var tag = _tagService.RegisterTag(name.Trim(), colorHex, isCleaningEnabled);
            Tags.Add(tag);
        }

        public void EditTag(TagModel tag, string name, string colorHex, bool isCleaningEnabled)
        {
            tag.Name = name.Trim();
            tag.ColorHex = colorHex;
            tag.IsCleaningEnabled = isCleaningEnabled;
            _tagService.UpdateTag(tag);
        }

        #region Commands

        [RelayCommand]
        private void DeleteTag(TagModel? tag)
        {
            if (tag is null) return;

            _tagService.UnregisterTag(tag);
            Tags.Remove(tag);
        }

        #endregion
    }
}
