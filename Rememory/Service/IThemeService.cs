using Microsoft.UI.Xaml;
using System;

namespace Rememory.Service
{
    public interface IThemeService
    {
        event EventHandler<ElementTheme> ThemeChanged;
        ElementTheme Theme { get; }
        void ApplyTheme();
    }
}
