using Microsoft.UI.Xaml;
using Rememory.Helper.WindowBackdrop;
using Windows.Foundation;

namespace Rememory.Contracts
{
    public interface IThemeService
    {
        event TypedEventHandler<IThemeService, ElementTheme> ThemeChanged;
        event TypedEventHandler<IThemeService, WindowBackdropType> WindowBackdropChanged;
        ElementTheme Theme { get; }
        WindowBackdropType WindowBackdrop { get; }
        void ApplyTheme();
        void ApplyWindowBackdrop();
    }
}
