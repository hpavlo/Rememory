using Microsoft.UI.Xaml;
using Rememory.Models;
using System;

namespace Rememory.Service
{
    public class ThemeService : IThemeService
    {
        private readonly SettingsContext _settingsContext = SettingsContext.Instance;

        public event EventHandler<ElementTheme> ThemeChanged;

        public ElementTheme Theme { get; private set; }

        public ThemeService()
        {
            Theme = GetTheme();
        }

        public void ApplyTheme()
        {
            Theme = GetTheme();
            ThemeChanged?.Invoke(this, Theme);
        }

        private ElementTheme GetTheme()
        {
            return (ElementTheme)_settingsContext.CurrentThemeIndex;
        }
    }
}
