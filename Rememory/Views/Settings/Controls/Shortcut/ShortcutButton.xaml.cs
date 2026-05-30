using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Rememory.Helper;
using Rememory.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Rememory.Views.Settings.Controls.Shortcut
{
    public sealed partial class ShortcutButton : UserControl
    {
        private readonly IKeyboardMonitor _keyboardMonitor = App.Current.Services.GetService<IKeyboardMonitor>()!;
        private readonly ContentDialog _dialogBox;
        private readonly ShortcutDialog _dialogContent;
        private readonly List<int> _currentPressedKeys = [];

        public IList<int> ActivationShortcut
        {
            get => (IList<int>)GetValue(ActivationShortcutProperty);
            set => SetValue(ActivationShortcutProperty, value);
        }

        public static readonly DependencyProperty ActivationShortcutProperty =
            DependencyProperty.Register("ActivationShortcut", typeof(IList<int>), typeof(ShortcutButton),
                new PropertyMetadata(default(IList<int>)));

        public IList<int> ActivationShortcutDefault
        {
            get => (IList<int>)GetValue(ActivationShortcutDefaultProperty);
            set => SetValue(ActivationShortcutDefaultProperty, value);
        }

        public static readonly DependencyProperty ActivationShortcutDefaultProperty =
            DependencyProperty.Register("ActivationShortcutDefault", typeof(IList<int>), typeof(ShortcutButton),
                new PropertyMetadata(default(IList<int>)));

        public ShortcutButton()
        {
            InitializeComponent();

            _dialogContent = new ShortcutDialog();
            _dialogBox = new()
            {
                Title = "/Settings/ShortcutDialog_Title/Text".GetLocalizedResource(),
                PrimaryButtonText = "Save".GetLocalizedResource(),
                SecondaryButtonText = "Reset".GetLocalizedResource(),
                CloseButtonText = "Cancel".GetLocalizedResource(),
                DefaultButton = ContentDialogButton.Primary,
                Content = _dialogContent
            };

            _dialogBox.Loading += DialogBox_Loading;
        }

        private void DialogBox_Loading(FrameworkElement sender, object args)
        {
            /// Dialog can invoke Loading few times
            /// Always unsubscribe first to clear any previous registrations
            _dialogContent.GotFocus -= DialogContent_GotFocus;
            _dialogContent.LostFocus -= DialogContent_LostFocus;

            _dialogContent.GotFocus += DialogContent_GotFocus;
            _dialogContent.LostFocus += DialogContent_LostFocus;
        }

        private void ShortcutButton_Unloaded(object sender, RoutedEventArgs e)
        {
            _dialogBox.Loading -= DialogBox_Loading;
            _dialogContent.GotFocus -= DialogContent_GotFocus;
            _dialogContent.LostFocus -= DialogContent_LostFocus;
            _keyboardMonitor.KeyboardEvent -= KeyboardMonitor_KeyboardEvent;

            Bindings.StopTracking();
        }

        private void DialogContent_GotFocus(object sender, RoutedEventArgs e)
        {
            _keyboardMonitor.KeyboardEvent -= KeyboardMonitor_KeyboardEvent;   // Clean up just in case
            _keyboardMonitor.KeyboardEvent += KeyboardMonitor_KeyboardEvent;
        }

        private void DialogContent_LostFocus(object sender, RoutedEventArgs e)
        {
            _keyboardMonitor.KeyboardEvent -= KeyboardMonitor_KeyboardEvent;
        }

        private async void ShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            _dialogContent.IsError = false;
            _dialogContent.ShortcutKeys = [.. KeyboardHelper.SortShortcut(ActivationShortcut)];

            _dialogBox.XamlRoot = XamlRoot;
            _dialogBox.RequestedTheme = App.Current.ThemeService.Theme;
            _dialogBox.IsPrimaryButtonEnabled = true;
            var result = await _dialogBox.ShowAsync();

            switch (result)
            {
                case ContentDialogResult.Primary:
                        ActivationShortcut = [.. KeyboardHelper.SortShortcut(_dialogContent.ShortcutKeys)];
                        break;
                case ContentDialogResult.Secondary:
                        ActivationShortcut = ActivationShortcutDefault;
                        break;
            }
        }

        private void KeyboardMonitor_KeyboardEvent(object? sender, GlobalKeyboardHookEventArgs e)
        {
            // If the button is unloaded, this control should be 'dead' to the monitor
            if (XamlRoot is null)
            {
                _keyboardMonitor.KeyboardEvent -= KeyboardMonitor_KeyboardEvent;
                return;
            }

            var key = FilterModifierKeys(e.KeyboardData.VirtualCode);

            if (key == (int)VirtualKey.Tab ||
                key == (int)VirtualKey.NumberKeyLock ||
                FocusManager.GetFocusedElement(XamlRoot).GetType() == typeof(Button))
                return;

            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown ||
                e.KeyboardState == GlobalKeyboardHook.KeyboardState.SysKeyDown)
            {
                if (!_currentPressedKeys.Contains(key))
                {
                    _currentPressedKeys.Add(key);
                }

                _dialogContent.ShortcutKeys = [.. _currentPressedKeys];
                if (IsShortcutValid(_dialogContent.ShortcutKeys))
                {
                    _dialogContent.IsError = false;
                    _dialogBox.IsPrimaryButtonEnabled = true;
                }
                else
                {
                    _dialogContent.IsError = true;
                    _dialogBox.IsPrimaryButtonEnabled = false;
                }
            }
            else if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyUp ||
                     e.KeyboardState == GlobalKeyboardHook.KeyboardState.SysKeyUp)
            {
                _currentPressedKeys.Remove(key);
            }

            e.Handled = true;
        }

        private static int FilterModifierKeys(int key)
        {
            return (VirtualKey)key switch
            {
                VirtualKey.LeftShift or VirtualKey.RightShift => (int)VirtualKey.Shift,
                VirtualKey.LeftControl or VirtualKey.RightControl => (int)VirtualKey.Control,
                VirtualKey.LeftMenu or VirtualKey.RightMenu => (int)VirtualKey.Menu,
                VirtualKey.LeftWindows or VirtualKey.RightWindows => (int)VirtualKey.LeftWindows,
                _ => key,
            };
        }

        private static bool IsShortcutValid(IList<int> shortcut)
        {
            return shortcut.Count > 1 &&
                KeyboardHelper.ModifierKeys.Contains((VirtualKey)shortcut.First()) &&
                !KeyboardHelper.ModifierKeys.Contains((VirtualKey)shortcut.Last());
        }
    }
}
