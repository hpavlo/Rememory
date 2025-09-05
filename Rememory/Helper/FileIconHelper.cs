using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Rememory.Helper
{
    public static class FileIconHelper
    {
        public static async Task<SoftwareBitmapSource?> GetFileIconAsync(string path, int size = 16)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must not be null or empty.", nameof(path));
            }

            IntPtr hIcon = IntPtr.Zero;
            try
            {
                hIcon = GetShellHIcon(path, size);
                if (hIcon == IntPtr.Zero)
                {
                    return null;
                }

                // Render HICON into a 32bpp DIB and copy pixels
                var pixels = RenderIconToBgra32(hIcon, size, size);

                // Create a SoftwareBitmap (BGRA8, premultiplied alpha) and push pixels
                var softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, size, size, BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pixels.AsBuffer());

                var softwareBitmapSource = new SoftwareBitmapSource();
                await softwareBitmapSource.SetBitmapAsync(softwareBitmap);
                softwareBitmap.Dispose();

                return softwareBitmapSource;
            }
            finally
            {
                if (hIcon != IntPtr.Zero)
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        // Get the HICON for file/folder using SHGetFileInfo
        private static IntPtr GetShellHIcon(string path, int size)
        {
            uint flags = SHGFI_ICON;

            if (!Path.Exists(path))
            {
                return IntPtr.Zero;
            }

            // Ask for small or large icon via SHGFI_SMALLICON.
            // For "16x16", SMALLICON is appropriate; for other sizes, DrawIconEx scales.
            if (size <= 16)
            {
                flags |= SHGFI_SMALLICON;
            }

            var result = SHGetFileInfo(path, 0, out SHFILEINFO shinfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);
            if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            return shinfo.hIcon;
        }

        // Draw the icon into a top-down 32bpp DIB using GDI, then read back pixels
        private static byte[] RenderIconToBgra32(IntPtr hIcon, int width, int height)
        {
            // Create a 32bpp top-down DIB
            var bmi = new BITMAPINFO
            {
                bmiHeader = new BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height, // top-down
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = BI_RGB,
                    biSizeImage = (uint)(width * height * 4)
                },
                // Ensure space for color masks (not used for BI_RGB) to satisfy marshalling layout
                bmiColors = new uint[3]
            };

            IntPtr hDib = CreateDIBSection(IntPtr.Zero, ref bmi, DIB_RGB_COLORS, out nint bits, IntPtr.Zero, 0);
            if (hDib == IntPtr.Zero || bits == IntPtr.Zero)
            {
                throw new InvalidOperationException("CreateDIBSection failed.");
            }

            IntPtr hdc = IntPtr.Zero;
            IntPtr old = IntPtr.Zero;
            try
            {
                hdc = CreateCompatibleDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                {
                    throw new InvalidOperationException("CreateCompatibleDC failed.");
                }

                old = SelectObject(hdc, hDib);
                if (old == IntPtr.Zero)
                {
                    throw new InvalidOperationException("SelectObject failed.");
                }

                // Draw the icon scaled to requested size
                if (!DrawIconEx(hdc, 0, 0, hIcon, width, height, 0, IntPtr.Zero, DI_NORMAL))
                {
                    throw new InvalidOperationException("DrawIconEx failed.");
                }

                // Copy out pixels (BGRA premultiplied)
                var pixels = new byte[width * height * 4];
                Marshal.Copy(bits, pixels, 0, pixels.Length);
                return pixels;
            }
            finally
            {
                if (old != IntPtr.Zero)
                {
                    SelectObject(hdc, old);
                }

                if (hDib != IntPtr.Zero)
                {
                    DeleteObject(hDib);
                }

                if (hdc != IntPtr.Zero)
                {
                    DeleteDC(hdc);
                }
            }
        }

        // ---- WinAPI interop ----

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;

        private const uint BI_RGB = 0;
        private const uint DIB_RGB_COLORS = 0;
        private const uint DI_NORMAL = 0x0003;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] bmiColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateDIBSection(
            IntPtr hdc,
            ref BITMAPINFO pbmi,
            uint iUsage,
            out IntPtr ppvBits,
            IntPtr hSection,
            uint dwOffset);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DrawIconEx(
            IntPtr hdc,
            int xLeft,
            int yTop,
            IntPtr hIcon,
            int cxWidth,
            int cyHeight,
            uint istepIfAniCur,
            IntPtr hbrFlickerFreeDraw,
            uint diFlags);
    }
}
