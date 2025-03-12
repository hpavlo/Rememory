using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Rememory.Contracts;
using Rememory.Models;
using System.Windows.Input;

namespace Rememory.ViewModels
{
    public class SettingsStoragePageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;

        private IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>();

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
            EraseClipboardDataCommand = new RelayCommand(_clipboardService.DeleteAllItems);
            DeleteOwnerAppFilterCommand = new RelayCommand<OwnerAppFilter>(filter =>
            {
                SettingsContext.OwnerAppFilters.Remove(filter);
                SettingsContext.OwnerAppFiltersSave();
            });
        }
    }
}
