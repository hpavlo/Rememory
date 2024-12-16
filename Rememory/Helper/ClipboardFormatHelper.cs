using Microsoft.Windows.Storage;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Rememory.Helper
{
    public static class ClipboardFormatHelper
    {
        public static readonly Dictionary<ClipboardFormat, uint> DataTypeFormats = new()
        {
            { ClipboardFormat.Text, NativeHelper.CF_UNICODETEXT },
            { ClipboardFormat.Rtf, NativeHelper.RegisterClipboardFormat("Rich Text Format") },
            { ClipboardFormat.Html, NativeHelper.RegisterClipboardFormat("HTML Format") },
            { ClipboardFormat.Png, NativeHelper.RegisterClipboardFormat("PNG") }
        };

        public static unsafe readonly Dictionary<ClipboardFormat, Func<(IntPtr, ulong), string>> DataTypeToStringConverters = new()
        {
            { ClipboardFormat.Text, _ => Marshal.PtrToStringUni(_.Item1) },
            { ClipboardFormat.Rtf, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Rtf) },   // Marshal.PtrToStringUTF8
            { ClipboardFormat.Html, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Html) },   // Marshal.PtrToStringUTF8
            { ClipboardFormat.Png, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Png) }
        };

        public static unsafe readonly Dictionary<ClipboardFormat, Func<string, IntPtr>> DataTypeToUnmanagedConverters = new()
        {
            { ClipboardFormat.Text, _ => (IntPtr)Utf16StringMarshaller.ConvertToUnmanaged(_) },
            { ClipboardFormat.Rtf, ConvertFileToPointer },   // Utf8StringMarshaller
            { ClipboardFormat.Html, ConvertFileToPointer },   // Utf8StringMarshaller
            { ClipboardFormat.Png, ConvertFileToPointer }
        };

        public static ClipboardFormat? GetFormatKeyByValue(uint value)
        {
            foreach (var kvp in DataTypeFormats)
            {
                if (kvp.Value == value)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public static bool AreItemsEqual(ClipboardItem firstItem, ClipboardItem secondItem)
        {
            return firstItem.HashMap.TryGetValue(ClipboardFormat.Text, out var firstItemText) &&
                   secondItem.HashMap.TryGetValue(ClipboardFormat.Text, out var secondItemText) &&
                   firstItemText.SequenceEqual(secondItemText) ||
                   firstItem.HashMap.TryGetValue(ClipboardFormat.Png, out var firstItemPng) &&
                   secondItem.HashMap.TryGetValue(ClipboardFormat.Png, out var secondItemPng) &&
                   firstItemPng.SequenceEqual(secondItemPng);
        }
        public static unsafe IntPtr ConvertFileToPointer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return IntPtr.Zero;
            }

            FileInfo fileInfo = new(filePath);
            if (!fileInfo.Exists)
            {
                return IntPtr.Zero;
            }

            long fileSize = fileInfo.Length;
            IntPtr dataPointer = Marshal.AllocHGlobal((int)fileSize);
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            fs.Read(new Span<byte>((void*)dataPointer, (int)fileSize));

            return dataPointer;
        }

        public static unsafe string ConvertPointerToFile(IntPtr dataPointer, ulong dataSize, ClipboardFormat format)
        {
            var filePath = GenerateFilePath(format);
            using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
            fs.Write(new ReadOnlySpan<byte>((void*)dataPointer, (int)dataSize));
            return filePath;
        }

        public const string FILE_NAME_FORMAT = "{0:yyyyMMdd_HHmmssfff}";
        public const string RTF_FILE_NAME_FORMAT = $"{FILE_NAME_FORMAT}.rtf";
        public const string HTML_FILE_NAME_FORMAT = $"{FILE_NAME_FORMAT}.html";
        public const string PNG_FILE_NAME_FORMAT = $"{FILE_NAME_FORMAT}.png";

        public const string ROOT_HISTORY_FOLDER_NAME = "History";
        public const string RTF_FORMAT_FOLDER_NAME = "RtfFormat";
        public const string HTML_FORMAT_FOLDER_NAME = "HtmlFormat";
        public const string PNG_FORMAT_FOLDER_NAME = "PngFormat";

        public static readonly string RootHistoryFolderPath = Path.Combine(ApplicationData.GetDefault().LocalPath, ROOT_HISTORY_FOLDER_NAME);

        private static string GenerateFilePath(ClipboardFormat format)
        {
            var pngPathStr = format switch
            {
                ClipboardFormat.Rtf => Path.Combine(RootHistoryFolderPath, RTF_FORMAT_FOLDER_NAME, string.Format(RTF_FILE_NAME_FORMAT, DateTime.Now)),
                ClipboardFormat.Html => Path.Combine(RootHistoryFolderPath, HTML_FORMAT_FOLDER_NAME, string.Format(HTML_FILE_NAME_FORMAT, DateTime.Now)),
                ClipboardFormat.Png => Path.Combine(RootHistoryFolderPath, PNG_FORMAT_FOLDER_NAME, string.Format(PNG_FILE_NAME_FORMAT, DateTime.Now)),
                _ => throw new NotImplementedException()
            };

            var directoryName = Path.GetDirectoryName(pngPathStr);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            return pngPathStr;
        }
    }

    public enum ClipboardFormat
    {
        Text = 0,
        Rtf = 1,
        Html = 2,
        Png = 3
    }
}
