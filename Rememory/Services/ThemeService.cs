using Microsoft.UI.Xaml;
using Rememory.Contracts;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Windows.Foundation;

namespace Rememory.Services
{
    public class ThemeService : IThemeService
    {
        public event TypedEventHandler<IThemeService, ElementTheme>? ThemeChanged;
        public event TypedEventHandler<IThemeService, WindowBackdropType>? WindowBackdropChanged;

        public ElementTheme Theme { get; private set; }
        public WindowBackdropType WindowBackdrop { get; private set; }

        private readonly SettingsContext _settingsContext = App.Current.SettingsContext;

        public ThemeService()
        {
            Theme = _settingsContext.Theme;
            WindowBackdrop = _settingsContext.WindowBackdrop;
        }

        public void ApplyTheme()
        {
            Theme = _settingsContext.Theme;
            ThemeChanged?.Invoke(this, Theme);
        }

        public void ApplyWindowBackdrop()
        {
            WindowBackdrop = _settingsContext.WindowBackdrop;
            WindowBackdropChanged?.Invoke(this, WindowBackdrop);
        }
    }
}
