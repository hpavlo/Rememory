using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rememory.Models;
using Rememory.Service;
using Rememory.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Rememory.Views.Editor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditorRootPage : Page
    {
        public readonly EditorRootPageViewModel ViewModel;

        private IThemeService ThemeService => App.Current.ThemeService;
        private readonly Window _window;

        public EditorRootPage(Window window, ClipboardItem context)
        {
            _window = window;
            ViewModel = new EditorRootPageViewModel(context);
            this.InitializeComponent();

            _window.SetTitleBar(WindowTitleBar);

            ApplyTheme();
            ThemeService.ThemeChanged += (s, a) => ApplyTheme();
        }

        private void ApplyTheme()
        {
            RequestedTheme = ThemeService.Theme;
        }

        // CanUndo and CanRedo doesn't work with Binding
        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CanUndoButton.IsEnabled = EditorTextBox.CanUndo;
            CanRedoButton.IsEnabled = EditorTextBox.CanRedo;
        }

        private unsafe void PresenterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_window.AppWindow.Presenter is CompactOverlayPresenter)
            {
                _window.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
            }
            else
            {
                _window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            }
        }
    }
}
