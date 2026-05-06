using Rememory.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Rememory.Hooks
{
    public sealed partial class KeyboardMonitor : IKeyboardMonitor, IDisposable
    {
        public event EventHandler<GlobalKeyboardHookEventArgs>? KeyboardEvent;

        private readonly GlobalKeyboardHook _globalKeyboardHook;
        private List<int> _previouslyPressedKeys = [];
        private bool _activationShortcutPressed;

        public KeyboardMonitor()
        {
            _globalKeyboardHook = new GlobalKeyboardHook();
        }

        public void StartMonitor()
        {
            _globalKeyboardHook.AddKeyboardHook();
            _globalKeyboardHook.KeyboardHandler += GlobalKeyboardHook_KeyboardPressed;
        }

        public void StopMonitor()
        {
            _globalKeyboardHook.RemoveKeyboardHook();
            _globalKeyboardHook.KeyboardHandler -= GlobalKeyboardHook_KeyboardPressed;
        }

        private void GlobalKeyboardHook_KeyboardPressed(object? sender, GlobalKeyboardHookEventArgs args)
        {
            KeyboardEvent?.Invoke(this, args);
            if (args.Handled)
            {
                return;
            }
            
            var currentlyPressedKeys = new List<int>();
            var pressedKey = args.KeyboardData.VirtualCode;

            // If the last key pressed is a modifier key, then currentlyPressedKeys cannot possibly match with _activationKeys
            // because _activationKeys contains exactly 1 non-modifier key. Hence, there's no need to check if `name` is a
            // modifier key or to do any additional processing on it.
            if (args.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown ||
                args.KeyboardState == GlobalKeyboardHook.KeyboardState.SysKeyDown)
            {
                // Check pressed modifier keys.
                AddModifierKeys(currentlyPressedKeys);
                currentlyPressedKeys.Add(pressedKey);
            }

            currentlyPressedKeys.Sort();

            if (currentlyPressedKeys.Count == 0 && _previouslyPressedKeys.Count != 0)
            {
                // no keys pressed, we can enable activation shortcut again
                _activationShortcutPressed = false;
            }

            _previouslyPressedKeys = currentlyPressedKeys;

            if (currentlyPressedKeys.SequenceEqual(App.Current.SettingsContext.ActivationShortcut))
            {
                // avoid triggering this action multiple times as this will be called nonstop while keys are pressed
                if (!_activationShortcutPressed)
                {
                    _activationShortcutPressed = true;
                    var window = App.Current.ClipboardWindow;
                    if (window.Visible)
                    {
                        window.HideWindow();
                    }
                    else
                    {
                        window.ShowWindow();
                    }
                    args.Handled = true;
                }
                return;
            }

            var clipboardWindow = App.Current.ClipboardWindow;
            if (clipboardWindow.Visible && args.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                if (pressedKey == (int)VirtualKey.Escape)
                {
                    clipboardWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var rootPage = clipboardWindow.Content as Views.ClipboardRootPage;
                        bool flyoutClosed = rootPage?.HandleEscapeKey() ?? false;

                        if (!flyoutClosed)
                        {
                            clipboardWindow.HideWindow();
                        }
                    });
                    args.Handled = true;
                    return;
                }

                // Tab/Shift+Tab: Handle focus navigation
                if (pressedKey == (int)VirtualKey.Tab)
                {
                    bool isShiftPressed = currentlyPressedKeys.Contains((int)VirtualKey.Shift);
                    clipboardWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var direction = isShiftPressed 
                            ? Microsoft.UI.Xaml.Input.FocusNavigationDirection.Previous 
                            : Microsoft.UI.Xaml.Input.FocusNavigationDirection.Next;

                        var options = new Microsoft.UI.Xaml.Input.FindNextElementOptions()
                        {
                            SearchRoot = clipboardWindow.Content
                        };

                        Microsoft.UI.Xaml.Input.FocusManager.TryMoveFocus(direction, options);
                    });
                    args.Handled = true;
                    return;
                }

                // Enter/Shift+Enter: Handle paste actions
                if (pressedKey == (int)VirtualKey.Enter)
                {
                    clipboardWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var rootPage = clipboardWindow.Content as Views.ClipboardRootPage;
                        rootPage?.HandleEnterKey();
                    });
                    args.Handled = true;
                    return;
                }

                // Arrow keys: Handle navigation
                if (pressedKey == (int)VirtualKey.Up || 
                    pressedKey == (int)VirtualKey.Down || 
                    pressedKey == (int)VirtualKey.Left || 
                    pressedKey == (int)VirtualKey.Right)
                {
                    clipboardWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var rootPage = clipboardWindow.Content as Views.ClipboardRootPage;
                        rootPage?.HandleArrowKeyNavigation((VirtualKey)pressedKey);
                    });
                    args.Handled = true;
                    return;
                }

                // Ctrl+T: Toggle pin
                if (currentlyPressedKeys.Count == 2 &&
                    currentlyPressedKeys.Contains((int)VirtualKey.Control) &&
                    currentlyPressedKeys.Contains((int)VirtualKey.T))
                {
                    clipboardWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var viewModel = (clipboardWindow.Content as Views.ClipboardRootPage)?.ViewModel;
                        if (viewModel?.ToggleWindowPinnedCommand.CanExecute(null) ?? false)
                        {
                            viewModel.ToggleWindowPinnedCommand.Execute(null);
                        }
                    });
                    args.Handled = true;
                    return;
                }

                // Ctrl+M: Toggle clipboard monitoring
                if (currentlyPressedKeys.Count == 2 &&
                    currentlyPressedKeys.Contains((int)VirtualKey.Control) &&
                    currentlyPressedKeys.Contains((int)VirtualKey.M))
                {
                    clipboardWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        var viewModel = (clipboardWindow.Content as Views.ClipboardRootPage)?.ViewModel;
                        if (viewModel?.ToggleClipboardMonitoringEnabledCommand.CanExecute(null) ?? false)
                        {
                            viewModel.ToggleClipboardMonitoringEnabledCommand.Execute(null);
                        }
                    });
                    args.Handled = true;
                    return;
                }

                if (!clipboardWindow.Pinned && IsInputKey(pressedKey, currentlyPressedKeys))
                {
                    clipboardWindow.DispatcherQueue.TryEnqueue(() => clipboardWindow.HideWindow());
                    return;
                }
            }
        }

        private static bool IsInputKey(int pressedKey, List<int> currentlyPressedKeys)
        {
            // Ignore pure modifier keys
            if (pressedKey == (int)VirtualKey.Control ||
                pressedKey == (int)VirtualKey.LeftControl ||
                pressedKey == (int)VirtualKey.RightControl ||
                pressedKey == (int)VirtualKey.Shift ||
                pressedKey == (int)VirtualKey.LeftShift ||
                pressedKey == (int)VirtualKey.RightShift ||
                pressedKey == (int)VirtualKey.Menu ||
                pressedKey == (int)VirtualKey.LeftMenu ||
                pressedKey == (int)VirtualKey.RightMenu ||
                pressedKey == (int)VirtualKey.LeftWindows ||
                pressedKey == (int)VirtualKey.RightWindows)
            {
                return false;
            }

            // Ignore if any modifier is pressed (could be a shortcut)
            if (currentlyPressedKeys.Contains((int)VirtualKey.Control) ||
                currentlyPressedKeys.Contains((int)VirtualKey.Menu) ||
                currentlyPressedKeys.Contains((int)VirtualKey.LeftWindows))
            {
                return false;
            }

            // Ignore special keys that don't represent input
            if (pressedKey == (int)VirtualKey.CapitalLock ||
                pressedKey == (int)VirtualKey.NumberKeyLock ||
                pressedKey == (int)VirtualKey.Scroll)
            {
                return false;
            }

            // Navigation keys should NOT trigger window hide
            if (pressedKey == (int)VirtualKey.Tab ||
                pressedKey == (int)VirtualKey.Enter ||
                pressedKey == (int)VirtualKey.Up ||
                pressedKey == (int)VirtualKey.Down ||
                pressedKey == (int)VirtualKey.Left ||
                pressedKey == (int)VirtualKey.Right ||
                pressedKey == (int)VirtualKey.Home ||
                pressedKey == (int)VirtualKey.End ||
                pressedKey == (int)VirtualKey.PageUp ||
                pressedKey == (int)VirtualKey.PageDown)
            {
                return false;
            }

            return true;
        }

        private void AddModifierKeys(List<int> currentlyPressedKeys)
        {
            if ((NativeHelper.GetAsyncKeyState((int)VirtualKey.Shift) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add((int)VirtualKey.Shift);
            }

            if ((NativeHelper.GetAsyncKeyState((int)VirtualKey.Control) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add((int)VirtualKey.Control);
            }

            if ((NativeHelper.GetAsyncKeyState((int)VirtualKey.Menu) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add((int)VirtualKey.Menu);
            }

            if ((NativeHelper.GetAsyncKeyState((int)VirtualKey.LeftWindows) & 0x8000) != 0 ||
                (NativeHelper.GetAsyncKeyState((int)VirtualKey.RightWindows) & 0x8000) != 0)
            {
                currentlyPressedKeys.Add((int)VirtualKey.LeftWindows);
            }
        }

        public void Dispose()
        {
            _globalKeyboardHook?.Dispose();
        }
    }
}
