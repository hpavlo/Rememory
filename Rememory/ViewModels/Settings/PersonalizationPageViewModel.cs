using CommunityToolkit.Mvvm.ComponentModel;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.Views;
using System.ComponentModel;

namespace Rememory.ViewModels.Settings
{
    public partial class PersonalizationPageViewModel : ObservableObject
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;
        public bool IsBackgropSupported => WindowBackdropHelper.IsSystemBackdropSupported;
        public bool IsWindowHeightEditorEnabled => SettingsContext.WindowPosition != ClipboardWindowPosition.Right;
        public bool IsWindowMarginEditorEnabled => SettingsContext.WindowPosition != ClipboardWindowPosition.ScreenCenter
            && SettingsContext.WindowPosition != ClipboardWindowPosition.LastPosition;

        public PersonalizationPageViewModel()
        {
            SettingsContext.PropertyChanged += SettingsContext_PropertyChanged;
        }

        private void SettingsContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsContext.WindowPosition))
            {
                OnPropertyChanged(nameof(IsWindowHeightEditorEnabled));
                OnPropertyChanged(nameof(IsWindowMarginEditorEnabled));
            }
        }
    }
}
