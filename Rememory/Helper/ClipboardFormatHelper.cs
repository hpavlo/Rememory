using Microsoft.Windows.Storage;
using Rememory.Models;
using Rememory.Models.NewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Rememory.Helper
{
    public static class ClipboardFormatHelper
    {
        public const string ROOT_HISTORY_FOLDER_NAME = "History";
        public const string FORMAT_FOLDER_NAME_RTF = "RtfFormat";
        public const string FORMAT_FOLDER_NAME_HTML = "HtmlFormat";
        public const string FORMAT_FOLDER_NAME_PNG = "PngFormat";

        public const string FILE_NAME_FORMAT = "{0:yyyyMMdd_HHmmssfff}";
        public const string FILE_NAME_FORMAT_RTF = $"{FILE_NAME_FORMAT}.rtf";
        public const string FILE_NAME_FORMAT_HTML = $"{FILE_NAME_FORMAT}.html";
        public const string FILE_NAME_FORMAT_PNG = $"{FILE_NAME_FORMAT}.png";

        public static readonly string RootHistoryFolderPath = Path.Combine(ApplicationData.GetDefault().LocalPath, ROOT_HISTORY_FOLDER_NAME);

        /// <summary>
        /// Dictionary to map ClipboardFormat enum to native clipboard format IDs
        /// </summary>
        public static readonly Dictionary<ClipboardFormat, uint> DataTypeFormats = new()
        {
            { ClipboardFormat.Text, NativeHelper.CF_UNICODETEXT },
            { ClipboardFormat.Rtf, NativeHelper.RegisterClipboardFormat("Rich Text Format") },
            { ClipboardFormat.Html, NativeHelper.RegisterClipboardFormat("HTML Format") },
            { ClipboardFormat.Png, NativeHelper.RegisterClipboardFormat("PNG") }
        };

        /// <summary>
        /// Dictionary to map ClipboardFormat to functions that convert clipboard data to string format
        /// </summary>
        public static unsafe readonly Dictionary<ClipboardFormat, Func<(IntPtr, UIntPtr), string>> DataTypeToStringConverters = new()
        {
            { ClipboardFormat.Text, _ => Marshal.PtrToStringUni(_.Item1) },
            { ClipboardFormat.Rtf, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Rtf) },   // Marshal.PtrToStringUTF8
            { ClipboardFormat.Html, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Html) },   // Marshal.PtrToStringUTF8
            { ClipboardFormat.Png, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Png) }
        };

        /// <summary>
        /// Dictionary to map ClipboardFormat to functions that convert string data to unmanaged pointers
        /// </summary>
        public static unsafe readonly Dictionary<ClipboardFormat, Func<string, IntPtr>> DataTypeToUnmanagedConverters = new()
        {
            { ClipboardFormat.Text, _ => (IntPtr)Utf16StringMarshaller.ConvertToUnmanaged(_) },
            { ClipboardFormat.Rtf, ConvertFileToPointer },   // Utf8StringMarshaller
            { ClipboardFormat.Html, ConvertFileToPointer },   // Utf8StringMarshaller
            { ClipboardFormat.Png, ConvertFileToPointer }
        };

        /// <summary>
        /// Retrieves the ClipboardFormat enum value corresponding to the given clipboard format ID.
        /// </summary>
        /// <param name="formatId">The clipboard format ID.</param>
        /// <returns>The ClipboardFormat enum value, or null if not found</returns>
        public static ClipboardFormat? GetFormatKeyByValue(uint formatId)
        {
            foreach (var kvp in DataTypeFormats)
            {
                if (kvp.Value == formatId)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Compares two ClipboardItem objects for equality based on their text or PNG content
        /// </summary>
        /// <param name="firstItem">The first ClipboardItem</param>
        /// <param name="secondItem">The second ClipboardItem</param>
        /// <returns>True if the items are equal, false otherwise</returns>
        public static bool AreItemsEqual(ClipboardItem firstItem, ClipboardItem secondItem)    // To remove
        {
            return firstItem.HashMap.TryGetValue(ClipboardFormat.Text, out var firstItemText) &&
                   secondItem.HashMap.TryGetValue(ClipboardFormat.Text, out var secondItemText) &&
                   firstItemText.SequenceEqual(secondItemText) ||
                   firstItem.HashMap.TryGetValue(ClipboardFormat.Png, out var firstItemPng) &&
                   secondItem.HashMap.TryGetValue(ClipboardFormat.Png, out var secondItemPng) &&
                   firstItemPng.SequenceEqual(secondItemPng);
        }

        /// <summary>
        /// Compares two ClipModel objects for equality based on their text or PNG content
        /// </summary>
        /// <param name="firstModel">The first ClipModel</param>
        /// <param name="secondModel">The second ClipModel</param>
        /// <returns>True if the models are equal, false otherwise</returns>
        public static bool EqualDataTo(this ClipModel firstModel, ClipModel secondModel)
        {
            return firstModel.Data.TryGetValue(ClipboardFormat.Text, out var firstTextData)
                && secondModel.Data.TryGetValue(ClipboardFormat.Text, out var secondTextData)
                && firstTextData.Equals(secondTextData)
                || firstModel.Data.TryGetValue(ClipboardFormat.Png, out var firstPngData)
                && secondModel.Data.TryGetValue(ClipboardFormat.Png, out var secondPngData)
                && firstPngData.Equals(secondPngData);
        }

        /// <summary>
        /// Clears external data files associated with a ClipModel
        /// </summary>
        /// <param name="clipModel">The ClipModel whose external data files should be cleared</param>
        public static void ClearExternalDataFiles(this ClipModel clipModel)
        {
            foreach (var dataItem in clipModel.Data)
            {
                if (dataItem.Key != ClipboardFormat.Text)
                {
                    try
                    {
                        var fileInfo = new FileInfo(dataItem.Value.Data);
                        if (fileInfo.Exists)
                        {
                            fileInfo.Delete();
                        }
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Extracts the file name from a full file path.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        /// <returns>The file name.</returns>
        public static string ConvertFullPathToFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        /// <summary>
        /// Constructs a full file path from a file name and ClipboardFormat
        /// If file name is an absolute path, it will be converted to a file name and after constructed to a full path
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="format">The ClipboardFormat</param>
        /// <returns>The full file path</returns>
        public static string ConvertFileNameToFullPath(string fileName, ClipboardFormat format)
        {
            if (Path.IsPathRooted(fileName))
            {
                fileName = ConvertFullPathToFileName(fileName);
            }

            string formatFolderName = format switch
            {
                ClipboardFormat.Rtf => FORMAT_FOLDER_NAME_RTF,
                ClipboardFormat.Html => FORMAT_FOLDER_NAME_HTML,
                ClipboardFormat.Png => FORMAT_FOLDER_NAME_PNG,
                _ => throw new NotImplementedException()
            };

            return Path.Combine(RootHistoryFolderPath, formatFolderName, fileName);
        }

        /// <summary>
        /// Converts a file to an unmanaged pointer containing the file's binary data
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>An <see cref="IntPtr"/> to the unmanaged memory, or <see cref="IntPtr.Zero"/> if an error occurs</returns>
        private static unsafe IntPtr ConvertFileToPointer(string filePath)
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

            try
            {
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
                fs.Read(new Span<byte>((void*)dataPointer, (int)fileSize));
            }
            catch (Exception)
            {
                Marshal.FreeHGlobal(dataPointer);
                return IntPtr.Zero;
            }

            return dataPointer;
        }

        /// <summary>
        /// Converts an unmanaged pointer containing binary data to a file
        /// </summary>
        /// <param name="dataPointer">The pointer to the binary data</param>
        /// <param name="dataSize">The size of the binary data</param>
        /// <param name="format">The ClipboardFormat of the data</param>
        /// <returns>The path to the created file</returns>
        private static unsafe string ConvertPointerToFile(IntPtr dataPointer, UIntPtr dataSize, ClipboardFormat format)
        {
            string filePath = GenerateFilePath(format);

            try
            {
                using FileStream fs = new(filePath, FileMode.Create, FileAccess.Write);
                fs.Write(new ReadOnlySpan<byte>((void*)dataPointer, (int)dataSize));
            }
            catch (Exception)
            {
                return string.Empty;
            }

            return filePath;
        }

        // Helper method to generate a unique file path for a given ClipboardFormat
        private static string GenerateFilePath(ClipboardFormat format)
        {
            string filePath = format switch
            {
                ClipboardFormat.Rtf => Path.Combine(RootHistoryFolderPath, FORMAT_FOLDER_NAME_RTF, string.Format(FILE_NAME_FORMAT_RTF, DateTime.Now)),
                ClipboardFormat.Html => Path.Combine(RootHistoryFolderPath, FORMAT_FOLDER_NAME_HTML, string.Format(FILE_NAME_FORMAT_HTML, DateTime.Now)),
                ClipboardFormat.Png => Path.Combine(RootHistoryFolderPath, FORMAT_FOLDER_NAME_PNG, string.Format(FILE_NAME_FORMAT_PNG, DateTime.Now)),
                _ => throw new NotImplementedException()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            return filePath;
        }
    }

    public enum ClipboardFormat
    {
        [Description("CF_UNICODETEXT")]
        Text,
        [Description("Rich Text Format")]
        Rtf,
        [Description("HTML Format")]
        Html,
        [Description("PNG")]
        Png
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
                if (attribute != null)
                {
                    return attribute.Description;
                }
            }
            return value.ToString();
        }

        public static T? FromDescription<T>(string description) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                if (value.GetDescription().Equals(description))
                {
                    return value;
                }
            }
            return default;
        }
    }
}
