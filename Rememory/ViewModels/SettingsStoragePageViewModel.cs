using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Models;
using Rememory.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rememory.ViewModels
{
    public partial class SettingsStoragePageViewModel : ObservableObject
    {
        private readonly IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;
        private readonly ITagService _tagService = App.Current.Services.GetService<ITagService>()!;

        public SettingsContext SettingsContext => SettingsContext.Instance;

        public ObservableCollection<TagModel> Tags;

        public bool IsRetentionPeriodParametersEnabled => SettingsContext.CleanupTypeIndex == (int)CleanupType.RetentionPeriod;
        public bool IsQuantityParametersEnabled => SettingsContext.CleanupTypeIndex == (int)CleanupType.Quantity;

        public int CleanupTypeIndex
        {
            get => SettingsContext.CleanupTypeIndex;
            set
            {
                if (SettingsContext.CleanupTypeIndex != value) {
                    SettingsContext.CleanupTypeIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsRetentionPeriodParametersEnabled));
                    OnPropertyChanged(nameof(IsQuantityParametersEnabled));
                }
            }
        }

        public SettingsStoragePageViewModel()
        {
            Tags = [.. _tagService.Tags];
        }

        [RelayCommand]
        public void EraseClipboardData() => _clipboardService.DeleteAllClips();

        [RelayCommand]
        public void DeleteOwnerAppFilter(OwnerAppFilter? filter)
        {
            if (filter is null) return;

            SettingsContext.OwnerAppFilters.Remove(filter);
            SettingsContext.OwnerAppFiltersSave();
        }

        [RelayCommand]
        public void DeleteTag(TagModel? tag)
        {
            if (tag is null) return;

            _tagService.UnregisterTag(tag);
            Tags.Remove(tag);
        }

        public void AddOwnerAppFilter(string name, string pattern)
        {
            SettingsContext.OwnerAppFilters.Add(new(name.Trim(), pattern.Trim()));
            SettingsContext.OwnerAppFiltersSave();
        }

        public void EditOwnerAppFilter(OwnerAppFilter filter, string newName, string newPattern)
        {
            filter.Name = newName.Trim();

            newPattern = newPattern.Trim();
            if (!newPattern.Equals(filter.Pattern))
            {
                filter.Pattern = newPattern;
                filter.FilteredCount = 0;
            }

            SettingsContext.OwnerAppFiltersSave();
        }

        public void AddTag(string name)
        {
            _tagService.RegisterTag(name.Trim());
            Tags.Add(_tagService.Tags.Where(t => t.Name.Equals(name.Trim())).First());
        }

        public void EditTag(TagModel tag, string name)
        {
            tag.Name = name.Trim();
            _tagService.UpdateTag(tag);
        }
    }
}
