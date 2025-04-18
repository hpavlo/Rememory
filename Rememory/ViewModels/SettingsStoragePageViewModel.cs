using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Models;
using Rememory.Services;
using System.Windows.Input;

namespace Rememory.ViewModels
{
    public class SettingsStoragePageViewModel : ObservableObject
    {
        private readonly IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;

        public SettingsContext SettingsContext => SettingsContext.Instance;

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
            InitializeCommands();
        }

        public void AddOwnerAppFilter(string name, string pattern)
        {
            SettingsContext.OwnerAppFilters.Add(new(name.Trim(), pattern.Trim()));
            SettingsContext.OwnerAppFiltersSave();
        }

        public void EditOwnerAppFilter(OwnerAppFilter filter, string newName, string newPattern)
        {
            newName = newName.Trim();
            newPattern = newPattern.Trim();

            if (!newName.Equals(filter.Name))
            {
                filter.Name = newName;
            }
            if (!newPattern.Equals(filter.Pattern))
            {
                filter.Pattern = newPattern;
                filter.FilteredCount = 0;
            }

            SettingsContext.OwnerAppFiltersSave();
        }

        public ICommand EraseClipboardDataCommand { get; private set; }
        public ICommand DeleteOwnerAppFilterCommand { get; private set; }

        private void InitializeCommands()
        {
            EraseClipboardDataCommand = new RelayCommand(_clipboardService.DeleteAllClips);
            DeleteOwnerAppFilterCommand = new RelayCommand<OwnerAppFilter>(filter =>
            {
                SettingsContext.OwnerAppFilters.Remove(filter);
                SettingsContext.OwnerAppFiltersSave();
            });
        }
    }
}
