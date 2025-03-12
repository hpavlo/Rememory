using Microsoft.UI.Xaml;
using Rememory.Helper.WindowBackdrop;
using System;

namespace Rememory.Contracts
{
    public interface IThemeService
    {
        event EventHandler<ElementTheme> ThemeChanged;
        event EventHandler<WindowBackdropType> WindowBackdropChanged;
        ElementTheme Theme { get; }
        WindowBackdropType WindowBackdrop { get; }
        void ApplyTheme();
        void ApplyWindowBackdrop();
    }
}
