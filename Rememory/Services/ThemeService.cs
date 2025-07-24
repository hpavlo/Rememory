using Microsoft.UI.Xaml;
using Rememory.Contracts;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using System;

namespace Rememory.Services
{
    public class ThemeService : IThemeService
    {
        private readonly SettingsContext _settingsContext = SettingsContext.Instance;

        public event EventHandler<ElementTheme>? ThemeChanged;
        public event EventHandler<WindowBackdropType>? WindowBackdropChanged;

        public ElementTheme Theme { get; private set; }
        public WindowBackdropType WindowBackdrop { get; private set; }

        public ThemeService()
        {
            Theme = GetTheme();
            WindowBackdrop = GetWindowBackdrop();
        }

        public void ApplyTheme()
        {
            Theme = GetTheme();
            ThemeChanged?.Invoke(this, Theme);
        }

        public void ApplyWindowBackdrop()
        {
            WindowBackdrop = GetWindowBackdrop();
            WindowBackdropChanged?.Invoke(this, WindowBackdrop);
        }

        private ElementTheme GetTheme()
        {
            return _settingsContext.Theme;
        }

        private WindowBackdropType GetWindowBackdrop()
        {
            return _settingsContext.WindowBackdrop;
        }
    }
}
