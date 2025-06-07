using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Rememory.Helper;
using Rememory.Models;
using Windows.ApplicationModel;
using WinUIEx;

namespace Rememory.Views.Editor
{
    public class EditorWindow
    {
        private static Window? _window;
        private static ClipModel? _clipContext;

        private EditorWindow() { }

        public static void ShowEditorWindow(ClipModel context)
        {
            if (_window is null)
            {
                _clipContext = context;
                _clipContext.IsOpenInEditor = true;
                InitializeWindow();
                _window!.Activate();
            }
            else
            {
                _window.Activate();
            }
        }

        public static void CloseEditorWindow()
        {
            _window?.Close();
        }

        public static bool TryGetEditorContext(out ClipModel? context)
        {
            context = _clipContext;
            return _clipContext is not null;
        }

        private static void InitializeWindow()
        {
            if (_clipContext is null) return;

            _window = new WindowEx()
            {
                MinHeight = 400,
                MinWidth = 400,
                ExtendsContentIntoTitleBar = true,
                SystemBackdrop = new MicaBackdrop(),
            };
            _window.Content = new EditorRootPage(_window, _clipContext);
            _window.Closed += EditorWindow_Closed;

            _window.AppWindow.Title = "EditorWindow_Title".GetLocalizedFormatResource(AppInfo.Current.DisplayInfo.DisplayName);
            _window.AppWindow.SetIcon("Assets\\WindowIcon.ico");
            _window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            _window.CenterOnScreen();
        }

        private static void EditorWindow_Closed(object sender, WindowEventArgs args)
        {
            if (_window is not null)
            {
                _window.Closed -= EditorWindow_Closed;
                _window = null;
            }

            if (_clipContext is not null)
            {
                _clipContext.IsOpenInEditor = false;
                _clipContext = null;
            }
        }
    }
}
