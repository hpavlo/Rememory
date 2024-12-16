using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Rememory.Hooks
{
    public sealed class KeyboardMonitor : IKeyboardMonitor, IDisposable
    {
        public event EventHandler<GlobalKeyboardHookEventArgs> KeyboardEvent;

        private SettingsContext SettingsContext => SettingsContext.Instance;
        private GlobalKeyboardHook _globalKeyboardHook;
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

        private void GlobalKeyboardHook_KeyboardPressed(object sender, GlobalKeyboardHookEventArgs args)
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

            if (currentlyPressedKeys.SequenceEqual(SettingsContext.ActivationShortcut))
            {
                // avoid triggering this action multiple times as this will be called nonstop while keys are pressed
                if (!_activationShortcutPressed)
                {
                    _activationShortcutPressed = true;
                    App.Current.ShowClipboardWindow();
                    args.Handled = true;
                }
            }
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
