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
        private static Window _window;
        private static ClipboardItem _itemContext;

        private EditorWindow() { }

        public static void ShowEditorWindow(ClipboardItem context)
        {
            if (_window is null)
            {
                _itemContext = context;
                _itemContext.IsOpenInEditor = true;
                InitializeWindow();
                _window.Activate();
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

        public static bool TryGetEditorContext(out ClipboardItem context)
        {
            context = _itemContext;
            return _itemContext is not null;
        }

        private static void InitializeWindow()
        {
            _window = new WindowEx()
            {
                MinHeight = 400,
                MinWidth = 400,
                ExtendsContentIntoTitleBar = true,
                SystemBackdrop = new MicaBackdrop(),
            };
            _window.Content = new EditorRootPage(_window, _itemContext);
            _window.Closed += EditorWindow_Closed;

            _window.AppWindow.Title = "EditorWindow_Title".GetLocalizedFormatResource(AppInfo.Current.DisplayInfo.DisplayName);
            _window.AppWindow.SetIcon("Assets\\WindowIcon.ico");
            _window.AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            _window.CenterOnScreen();
        }

        private static void EditorWindow_Closed(object sender, WindowEventArgs args)
        {
            _window = null;
            if (_itemContext is not null)
            {
                _itemContext.IsOpenInEditor = false;
                _itemContext = null;
            }
        }
    }
}
