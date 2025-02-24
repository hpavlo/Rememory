﻿using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Rememory.Helper;
using Rememory.Helper.WindowBackdrop;
using Rememory.Models;
using Rememory.Views.Settings;
using System;
using System.Drawing;
using Windows.ApplicationModel;
using Windows.Graphics;
using Windows.System;
using WinUIEx;
using WinUIEx.Messaging;

namespace Rememory.Views
{
    public class ClipboardWindow : WindowEx
    {
        public SettingsContext SettingsContext => SettingsContext.Instance;
        public bool IsPinned { get; set; } = false;

        public event EventHandler Showing;
        public event EventHandler Hiding;

        private WindowMessageMonitor _messageMonitor;

        public ClipboardWindow()
        {
            Title = Package.Current.DisplayName;
            IsShownInSwitchers = false;
            IsAlwaysOnTop = true;
            IsResizable = false;
            IsMaximizable = false;
            IsMinimizable = false;
            TaskBarIcon = Icon.FromFile(AppContext.BaseDirectory + "Assets\\WindowIcon.ico");

            this.SetWindowStyle(WindowStyle.Popup);
            int cornerPreference = (int)NativeHelper.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            NativeHelper.DwmSetWindowAttribute(this.GetWindowHandle(), NativeHelper.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

            _messageMonitor = new WindowMessageMonitor(this.GetWindowHandle());
            _messageMonitor.WindowMessageReceived += WindowMessageReceived;

            this.Activated += Window_Activated;
            this.AppWindow.Closing += Window_Closing;
            this.Closed += Window_Closed;
        }

        public bool ShowWindow()
        {
            if (this.Visible)
            {
                this.SetForegroundWindow();
                return false;
            }
            MoveToStartPosition();
            Showing?.Invoke(this, EventArgs.Empty);
            this.AppWindow.Show();
            KeyboardHelper.MultiKeyAction([(VirtualKey)0x0E], KeyboardHelper.KeyAction.DownUp);   // To fix problem with foreground window
            this.SetForegroundWindow();
            return true;
        }

        public bool HideWindow()
        {
            if (!this.Visible)
            {
                return false;
            }
            Hiding?.Invoke(this, EventArgs.Empty);
            this.AppWindow.Hide();
            return true;
        }

        // Call it after set Content of window
        public bool InitSystemBackdrop()
        {
            if (WindowBackdropHelper.IsSystemBackdropSupported)
            {
                var backdropHelper = new WindowBackdropHelper(this);
                return backdropHelper.InitWindowBackdrop();
            }
            return false;
        }

        private void WindowMessageReceived(object sender, WindowMessageEventArgs args)
        {
            switch (args.Message.MessageId)
            {
                case NativeHelper.WM_QUERYENDSESSION:
                    if (args.Message.LParam == 1)   // ENDSESSION_CLOSEAPP
                    {
                        NativeHelper.RegisterApplicationRestart(null, 0x1011);   // RESTART_NO_CRASH  | RESTART_NO_HANG  | RESTART_NO_REBOOT
                    }
                    args.Result = 1;
                    args.Handled = true;
                    break;

                case NativeHelper.WM_ENDSESSION:
                    if (args.Message.WParam != 0)   // wParam = 1 means the session is ending
                    {
                        App.Current.Exit();
                    }
                    break;

                case NativeHelper.WM_COMMAND:
                    switch (args.Message.WParam)
                    {
                        case RememoryCoreHelper.TRAY_OPEN_COMMAND:
                            ShowWindow();
                            break;
                        case RememoryCoreHelper.TRAY_SETTINGS_COMMAND:
                            SettingsWindow.ShowSettingsWindow();
                            break;
                        case RememoryCoreHelper.TRAY_EXIT_COMMAND:
                            App.Current.Exit();
                            break;
                    }
                    break;
                case RememoryCoreHelper.TRAY_NOTIFICATION:
                    if (args.Message.LParam == NativeHelper.WM_LBUTTONUP)
                        ShowWindow();
                    break;
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (!IsPinned && args.WindowActivationState == WindowActivationState.Deactivated)
            {
                HideWindow();
            }
        }

        private void Window_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            HideWindow();
            args.Cancel = true;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            SettingsWindow.CloseSettingsWindow();
            this.Activated -= Window_Activated;
            _messageMonitor.WindowMessageReceived -= WindowMessageReceived;
        }

        private void MoveToStartPosition()
        {
            var workArea = GetWorkAreaRectangle();

            int width = SettingsContext.WindowWidth;
            // Temp height
            // Add this as settings parameter
            int height = 400;
            int margin = SettingsContext.WindowMargin;

            // To update DPI for window
            this.AppWindow.Move(new(workArea.Right - width - margin, workArea.Top + margin));

            TextBoxCaretHelper.GetCaretPosition(out var rect);
            PositionWindowRelativeToCaret(rect, workArea, width, height, margin);
        }

        private void PositionWindowRelativeToCaret(Rectangle caretRect, Rectangle workArea, int windowWidth, int windowHeight, int margin)
        {
            // Defoult position for window 
            int x = workArea.Right - windowWidth - margin;
            int y = workArea.Bottom - windowHeight - margin;

            if (!caretRect.IsEmpty)
            {
                if (workArea.Right - caretRect.Right > windowWidth)
                {
                    x = caretRect.Right;
                }
                else if (workArea.Right - caretRect.Left > windowWidth)
                {
                    x = caretRect.Left;
                }
                else
                {
                    x = workArea.Right - windowWidth;
                }

                if (workArea.Bottom - caretRect.Bottom > windowHeight)
                {
                    y = caretRect.Bottom;
                }
                else if (caretRect.Top - workArea.Top > windowHeight)
                {
                    y = caretRect.Top - windowHeight;
                }
                else if (workArea.Bottom - caretRect.Top > windowHeight)
                {
                    y = caretRect.Top;
                }
                else
                {
                    y = workArea.Bottom - windowHeight;
                }
            }

            this.AppWindow.MoveAndResize(new RectInt32(x, y, windowWidth, windowHeight));
        }

        private Rectangle GetWorkAreaRectangle()
        {
            NativeHelper.GetCursorPos(out NativeHelper.PointInter point);
            IntPtr monitor = NativeHelper.MonitorFromPoint(point, NativeHelper.MONITOR_DEFAULTTONEAREST);
            NativeHelper.MonitorInfoEx info = new();
            NativeHelper.GetMonitorInfo(monitor, info);

            return new Rectangle(
                info.rcWork.left,
                info.rcWork.top,
                info.rcWork.right - info.rcWork.left,
                info.rcWork.bottom - info.rcWork.top);
        }
    }
}
