using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Rememory.Helper
{
    public static partial class FileIconHelper
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
                var iconIndex = GetIconIndex(path);
                hIcon = GetSmallIcon(iconIndex);
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

        private static int GetIconIndex(string pszFile)
        {
            SHFILEINFO sfi = new();
            SHGetFileInfo(pszFile, 0, ref sfi, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
            return sfi.iIcon;
        }

        private static IntPtr GetSmallIcon(int iImage)
        {
            IImageList? spiml = null;
            var guid = new Guid(IID_IImageList);

            SHGetImageList(SHIL_SMALL, ref guid, ref spiml);
            IntPtr hIcon = IntPtr.Zero;
            spiml.GetIcon(iImage, ILD_TRANSPARENT | ILD_IMAGE, ref hIcon);

            var info = new IMAGEINFO();
            spiml.GetImageInfo(iImage, ref info);

            return hIcon;
        }

        /// <summary>
        /// Draw the icon into a top-down 32bpp DIB using GDI, then read back pixels
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
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

        #region COM Interface

        private const string IID_IImageList = "46EB5926-582E-4017-9FDF-E8998DAA0950";

        [ComImportAttribute()]
        [GuidAttribute(IID_IImageList)]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(ref IntPtr pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, ref IntPtr picon);

            [PreserveSig]
            int GetImageInfo(int i, ref IMAGEINFO pImageInfo);

            // Other methods
        }

        #endregion        

        #region WinAPI

        private const uint SHGFI_SYSICONINDEX = 0x000004000;
        private const uint SHGFI_SMALLICON = 0x000000001;

        private const int SHIL_SMALL = 0x1;
        private const int ILD_TRANSPARENT = 0x00000001;
        private const int ILD_IMAGE = 0x00000020;

        private const uint BI_RGB = 0;
        private const uint DIB_RGB_COLORS = 0;
        private const uint DI_NORMAL = 0x0003;

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGEINFO
        {
            public IntPtr hbmImage;
            public IntPtr hbmMask;
            public int Unused1;
            public int Unused2;
            public NativeHelper.Rect rcImage;
        }

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

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("shell32.dll", EntryPoint = "#727", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, ref IImageList ppv);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool DestroyIcon(IntPtr hIcon);

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

        #endregion
    }
}
