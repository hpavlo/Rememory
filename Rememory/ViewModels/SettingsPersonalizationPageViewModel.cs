using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;

namespace Rememory.ViewModels
{
    public partial class SettingsPersonalizationPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;
        public bool IsBackgropSupported => WindowBackdropHelper.IsSystemBackdropSupported;
    }
}
