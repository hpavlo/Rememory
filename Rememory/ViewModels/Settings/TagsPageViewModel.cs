using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using Rememory.Contracts;
using Rememory.Models;
using System.Collections.ObjectModel;
using System.Linq;

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

        public void AddTag(string name, SolidColorBrush colorBrush, bool isCleaningEnabled)
        {
            _tagService.RegisterTag(name.Trim(), colorBrush, isCleaningEnabled);
            Tags.Add(_tagService.Tags.Where(t => t.Name.Equals(name.Trim())).First());
        }

        public void EditTag(TagModel tag, string name, SolidColorBrush colorBrush, bool isCleaningEnabled)
        {
            tag.Name = name.Trim();
            tag.ColorBrush = colorBrush;
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
