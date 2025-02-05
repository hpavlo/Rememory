using System;
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
        public static extern bool SetDataToClipboard(ref ClipboardDataInfo dataInfo);


        [DllImport("Rememory.Core.dll")]
        public static extern bool AddWindowProc(IntPtr hWnd);

        [DllImport("Rememory.Core.dll")]
        public static extern bool CreateTrayIcon(IntPtr hWnd, IntPtr openMenuName, IntPtr settingsMenuName, IntPtr exitMenuName, IntPtr description);

        [DllImport("Rememory.Core.dll")]
        public static extern void UpdateTrayIconMenuItem(uint commandId, IntPtr newName);
    }

    public delegate bool ClipboardMonitorCallback(ref ClipboardDataInfo dataInfo);

    /// <summary>
    /// Information about all formats and clipboard owner
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ClipboardDataInfo
    {
        /// <summary>
        /// All <see cref="FormatDataItem"/> count
        /// </summary>
        public uint FormatCount;
        /// <summary>
        /// Pointer to the first <see cref="FormatDataItem"/>
        /// </summary>
        public IntPtr FirstItem;
        /// <summary>
        /// Pointer to the owner path
        /// </summary>
        public IntPtr OwnerPath;
        /// <summary>
        /// Length of the owner icon bitmap
        /// </summary>
        public int IconLength;
        /// <summary>
        /// Pointer to the owner icon bitmap
        /// </summary>
        public IntPtr IconPixels;
    };

    /// <summary>
    /// Information about ond format form clipboard
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FormatDataItem
    {
        /// <summary>
        /// Format code
        /// </summary>
        public uint Format;
        /// <summary>
        /// Pointer to the data
        /// </summary>
        public IntPtr Data;
        /// <summary>
        /// Data length
        /// </summary>
        public UIntPtr Size;
        /// <summary>
        /// Hash of the data
        /// </summary>
        public IntPtr Hash;
    }
}
