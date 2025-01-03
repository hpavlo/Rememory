﻿using System;
using System.Runtime.InteropServices;

namespace Rememory.Helper
{
    public class RememoryCoreHelper
    {
        public const uint TRAY_NOTIFICATION = NativeHelper.WM_USER + 1;
        public const uint TRAY_OPEN_COMMAND = 10;
        public const uint TRAY_SETTINGS_COMMAND = 11;
        public const uint TRAY_EXIT_COMMAND = 12;

        [DllImport("Rememory.Core.dll")]
        public static extern bool StartClipboardMonitor(IntPtr hWnd, ClipboardMonitorCallback handler);

        [DllImport("Rememory.Core.dll")]
        public static extern bool StopClipboardMonitor(IntPtr hWnd);

        [DllImport("Rememory.Core.dll")]
        public static extern bool SetDataToClipboard(ClipboardDataInfo dataInfo);


        [DllImport("Rememory.Core.dll")]
        public static extern bool AddWindowProc(IntPtr hWnd);

        [DllImport("Rememory.Core.dll")]
        public static extern bool CreateTrayIcon(IntPtr hWnd, IntPtr openMenuName, IntPtr settingsMenuName, IntPtr exitMenuName, IntPtr description);

        [DllImport("Rememory.Core.dll")]
        public static extern void UpdateTrayIconMenuItem(uint commandId, IntPtr newName);
    }

    public delegate bool ClipboardMonitorCallback(ClipboardDataInfo dataInfo);

    [StructLayout(LayoutKind.Sequential)]
    public struct ClipboardDataInfo
    {
        public uint FormatCount;
        public IntPtr FirstItem;
        public IntPtr OwnerPath;
        public int IconLength;
        public IntPtr IconPixels;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct FormatDataItem
    {
        public uint Format;
        public IntPtr Data;
        public ulong Size;
        public IntPtr Hash;
    }
}
