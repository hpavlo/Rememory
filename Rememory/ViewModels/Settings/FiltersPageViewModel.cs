using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rememory.Models;

namespace Rememory.ViewModels.Settings
{
    public partial class FiltersPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;

        public void AddOwnerAppFilter(string name, string pattern)
        {
            SettingsContext.OwnerAppFilters.Add(new(name.Trim(), pattern.Trim()));
            SettingsContext.SaveOwnerAppFilters();
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

            SettingsContext.SaveOwnerAppFilters();
        }

        #region Commands

        [RelayCommand]
        private void DeleteOwnerAppFilter(OwnerAppFilter? filter)
        {
            if (filter is null) return;

            SettingsContext.OwnerAppFilters.Remove(filter);
            SettingsContext.SaveOwnerAppFilters();
        }

        #endregion
    }
}
