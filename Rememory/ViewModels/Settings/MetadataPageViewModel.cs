using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Models;

namespace Rememory.ViewModels.Settings
{
    public partial class MetadataPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext { get; } = App.Current.SettingsContext;
    }
}
