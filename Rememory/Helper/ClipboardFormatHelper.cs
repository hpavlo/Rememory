using Microsoft.Windows.Storage;
using Rememory.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Rememory.Helper
{
    /// <summary>
    /// Provides constants, mappings, and utility functions for handling different clipboard formats,
    /// managing associated file storage, and converting between managed types and unmanaged clipboard data.
    /// </summary>
    public static class ClipboardFormatHelper
    {
        #region Constants for Folder Structure

        /// <summary>
        /// The root directory name within the application's local data folder where non-text clipboard data is stored.
        /// </summary>
        public const string ROOT_HISTORY_FOLDER_NAME = "History";

        /// <summary>
        /// The subfolder name within the history root for storing RTF format files.
        /// </summary>
        public const string FORMAT_FOLDER_NAME_RTF = "RtfFormat";

        /// <summary>
        /// The subfolder name within the history root for storing HTML format files.
        /// </summary>
        public const string FORMAT_FOLDER_NAME_HTML = "HtmlFormat";

        /// <summary>
        /// The subfolder name within the history root for storing PNG format files.
        /// </summary>
        public const string FORMAT_FOLDER_NAME_PNG = "PngFormat";

        /// <summary>
        /// The subfolder name within the history root for storing BITMAP format files.
        /// </summary>
        public const string FORMAT_FOLDER_NAME_BITMAP = "BitmapFormat";

        #endregion

        #region Constants for File Naming

        /// <summary>
        /// The base date/time format string used for generating unique filenames for stored clipboard data files.
        /// Format: YearMonthDay_HourMinuteSecondMillisecond (e.g., 20231027_153005123).
        /// </summary>
        public const string FILE_NAME_FORMAT = "{0:yyyyMMdd_HHmmssfff}";

        /// <summary>
        /// The complete filename format string for RTF files, including the extension.
        /// </summary>
        public const string FILE_NAME_FORMAT_RTF = $"{FILE_NAME_FORMAT}.rtf";

        /// <summary>
        /// The complete filename format string for HTML files, including the extension.
        /// </summary>
        public const string FILE_NAME_FORMAT_HTML = $"{FILE_NAME_FORMAT}.html";

        /// <summary>
        /// The complete filename format string for PNG files, including the extension.
        /// </summary>
        public const string FILE_NAME_FORMAT_PNG = $"{FILE_NAME_FORMAT}.png";

        /// <summary>
        /// The complete filename format string for BITMAP files, including the extension.
        /// </summary>
        public const string FILE_NAME_FORMAT_BITMAP = $"{FILE_NAME_FORMAT}.bmp";

        #endregion

        /// <summary>
        /// Gets the absolute path to the root directory used for storing non-text clipboard history files.
        /// Typically located within the application's local data folder (e.g., AppData\Local\...).
        /// </summary>
        public static readonly string RootHistoryFolderPath = Path.Combine(ApplicationData.GetDefault().LocalPath, ROOT_HISTORY_FOLDER_NAME);

        /// <summary>
        /// Dictionary mapping the application-defined <see cref="ClipboardFormat"/> enum
        /// to the corresponding native Windows clipboard format identifiers (UINT).
        /// Used for interacting with the native clipboard API.
        /// </summary>
        public static readonly Dictionary<ClipboardFormat, uint> DataTypeFormats = new()
        {
            { ClipboardFormat.Text, NativeHelper.CF_UNICODETEXT },
            { ClipboardFormat.Bitmap, NativeHelper.CF_BITMAP },
            { ClipboardFormat.Rtf, NativeHelper.RegisterClipboardFormat("Rich Text Format") },
            { ClipboardFormat.Html, NativeHelper.RegisterClipboardFormat("HTML Format") },
            { ClipboardFormat.Png, NativeHelper.RegisterClipboardFormat("PNG") }
        };

        /// <summary>
        /// Dictionary mapping <see cref="ClipboardFormat"/> to functions that convert raw clipboard data
        /// (represented by an unmanaged memory pointer and size) into a managed string representation.
        /// For non-text formats, this string is typically the path to a temporary file where the data was saved.
        /// </summary>
        public static unsafe readonly Dictionary<ClipboardFormat, Func<(IntPtr, UIntPtr), string>> DataTypeToStringConverters = new()
        {
            { ClipboardFormat.Text, _ => Marshal.PtrToStringUni(_.Item1) ?? string.Empty },
            { ClipboardFormat.Bitmap, _ => ConvertBitmapToFile(_.Item1) },
            { ClipboardFormat.Rtf, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Rtf) },   // Marshal.PtrToStringUTF8 to directly convert the data
            { ClipboardFormat.Html, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Html) },   // Marshal.PtrToStringUTF8
            { ClipboardFormat.Png, _ => ConvertPointerToFile(_.Item1, _.Item2, ClipboardFormat.Png) }
        };

        /// <summary>
        /// Dictionary mapping <see cref="ClipboardFormat"/> to functions that convert managed string data
        /// (either plain text or a file path for binary formats) into an unmanaged memory pointer (`IntPtr`)
        /// suitable for placing onto the native clipboard.
        /// </summary>
        public static unsafe readonly Dictionary<ClipboardFormat, Func<string, IntPtr>> DataTypeToUnmanagedConverters = new()
        {
            { ClipboardFormat.Text, _ => (IntPtr)Utf16StringMarshaller.ConvertToUnmanaged(_) },
            { ClipboardFormat.Bitmap, _ => (IntPtr)Utf16StringMarshaller.ConvertToUnmanaged(_) },
            { ClipboardFormat.Rtf, ConvertFileToPointer },   // Utf8StringMarshaller
            { ClipboardFormat.Html, ConvertFileToPointer },   // Utf8StringMarshaller
            { ClipboardFormat.Png, ConvertFileToPointer }
        };

        /// <summary>
        /// Retrieves the <see cref="ClipboardFormat"/> enum key corresponding to the given native clipboard format identifier.
        /// Performs a reverse lookup in the <see cref="DataTypeFormats"/> dictionary.
        /// </summary>
        /// <param name="formatId">The native clipboard format identifier (UINT).</param>
        /// <returns>The matching <see cref="ClipboardFormat"/> enum value, or <c>null</c> if no match is found.</returns>
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
        /// Compares two <see cref="ClipModel"/> objects for data equality.
        /// Equality is defined as having the same Text data hash OR the same PNG data hash.
        /// </summary>
        /// <param name="firstModel">The first ClipModel to compare.</param>
        /// <param name="secondModel">The second ClipModel to compare.</param>
        /// <returns><c>true</c> if the data content (Text or PNG hash) is considered equal; otherwise, <c>false</c>.</returns>
        public static bool EqualDataTo(this ClipModel firstModel, ClipModel secondModel)
        {
            // Check if both have Text data and their hashes match
            bool textMatch = firstModel.Data.TryGetValue(ClipboardFormat.Text, out var firstTextData)
                          && secondModel.Data.TryGetValue(ClipboardFormat.Text, out var secondTextData)
                          && StructuralComparisons.StructuralEqualityComparer.Equals(firstTextData?.Hash, secondTextData?.Hash);

            // If text matches, they are equal
            if (textMatch) return true;

            // Check if both have Bitmap data and their hashes match
            bool bitmapMatch = firstModel.Data.TryGetValue(ClipboardFormat.Bitmap, out var firstBitmapData)
                          && secondModel.Data.TryGetValue(ClipboardFormat.Bitmap, out var secondBitmapData)
                          && StructuralComparisons.StructuralEqualityComparer.Equals(firstBitmapData?.Hash, secondBitmapData?.Hash);

            // If bitmap matches, they are equal
            if (bitmapMatch) return true;

            // Check if both have Png data and their hashes match
            bool pngMatch = firstModel.Data.TryGetValue(ClipboardFormat.Png, out var firstPngData)
                         && secondModel.Data.TryGetValue(ClipboardFormat.Png, out var secondPngData)
                         && StructuralComparisons.StructuralEqualityComparer.Equals(firstPngData?.Hash, secondPngData?.Hash);

            // Return true if PNG matches (since text didn't match)
            return pngMatch;
        }

        /// <summary>
        /// Deletes external files associated with non-text data formats stored within a <see cref="ClipModel"/>.
        /// It iterates through the clip's data items and attempts to delete the file path stored in `DataModel.Data`
        /// for formats other than Text.
        /// </summary>
        /// <param name="clipModel">The <see cref="ClipModel"/> whose external data files should be deleted.</param>
        public static void ClearExternalDataFiles(this ClipModel clipModel)
        {
            var filesToDelete = clipModel.Data.Values.Where(IsFile).ToArray();

            foreach (var dataModel in filesToDelete)
            {
                try
                {
                    var fileInfo = new FileInfo(dataModel.Data);
                    if (fileInfo.Exists)
                    {
                        fileInfo.Delete();
                    }
                    clipModel.Data.Remove(dataModel.Format);
                }
                catch { }
            }
        }

        /// <summary>
        /// Attempts to delete all known external data format folders (RTF, HTML, PNG)
        /// located within the <see cref="RootHistoryFolderPath"/>.
        /// </summary>
        public static void ClearAllExternalData()
        {
            try
            {
                DeleteFolder(Path.Combine(RootHistoryFolderPath, FORMAT_FOLDER_NAME_RTF));
                DeleteFolder(Path.Combine(RootHistoryFolderPath, FORMAT_FOLDER_NAME_HTML));
                DeleteFolder(Path.Combine(RootHistoryFolderPath, FORMAT_FOLDER_NAME_PNG));
                DeleteFolder(Path.Combine(RootHistoryFolderPath, FORMAT_FOLDER_NAME_BITMAP));
            }
            catch { }

            void DeleteFolder(string path)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }

        /// <summary>
        /// Specifies whether data is stored in a file format.
        /// </summary>
        /// <param name="data">The data we want to check.</param>
        /// <returns><c>True</c> if data is saved as an external file, otherwise <c>False</c>.</returns>
        public static bool IsFile(this DataModel data)
        {
            return CanFormatBeFile(data.Format);
        }

        /// <summary>
        /// Specifies whether format can be stored as a file.
        /// </summary>
        /// <param name="format">The format we want to check</param>
        /// <returns><c>True</c> if data can be saved as an external file, otherwise <c>False</c>.</returns>
        public static bool CanFormatBeFile(ClipboardFormat format)
        {
            return format != ClipboardFormat.Text;
        }

        /// <summary>
        /// Extracts the file name (including extension) from a given full file path.
        /// </summary>
        /// <param name="filePath">The full path to the file (e.g., "C:\History\RtfFormat\file.rtf").</param>
        /// <returns>The file name part (e.g., "file.rtf"). Returns the original string if it's not a valid path.</returns>
        public static string ConvertFullPathToFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        /// <summary>
        /// Constructs the full, absolute path for storing an external clipboard data file
        /// based on its intended format and a potentially relative file name.
        /// If the input <paramref name="fileName"/> is already an absolute path,
        /// it extracts the file name part first before constructing the new path within the history structure.
        /// </summary>
        /// <param name="fileName">The base file name (e.g., "clip.rtf") or potentially a full path.</param>
        /// <param name="format">The <see cref="ClipboardFormat"/> determining the target subfolder (Rtf, Html, Png).</param>
        /// <returns>The full, absolute path within the application's history folder structure.</returns>
        /// <exception cref="NotImplementedException">Thrown if the <paramref name="format"/> is not handled (e.g., Text).</exception>
        public static string ConvertFileNameToFullPath(string fileName, ClipboardFormat format)
        {
            // If the input is already a full path, extract just the filename part.
            if (Path.IsPathRooted(fileName))
            {
                fileName = ConvertFullPathToFileName(fileName);
            }

            string formatFolderName = format switch
            {
                ClipboardFormat.Rtf => FORMAT_FOLDER_NAME_RTF,
                ClipboardFormat.Html => FORMAT_FOLDER_NAME_HTML,
                ClipboardFormat.Png => FORMAT_FOLDER_NAME_PNG,
                ClipboardFormat.Bitmap => FORMAT_FOLDER_NAME_BITMAP,
                // Explicitly handle Text or throw for unsupported formats intended for file storage.
                ClipboardFormat.Text => throw new ArgumentException("Text format should not be stored as an external file.", nameof(format)),
                _ => throw new NotImplementedException($"Folder mapping not implemented for format: {format}")
            };

            return Path.Combine(RootHistoryFolderPath, formatFolderName, fileName);
        }

        /// <summary>
        /// Reads the binary content of a specified file into unmanaged memory allocated via <see cref="Marshal.AllocHGlobal"/>.
        /// </summary>
        /// <param name="filePath">The path to the file to read.</param>
        /// <returns>
        /// An <see cref="IntPtr"/> pointing to the allocated unmanaged memory containing the file data.
        /// Returns <see cref="IntPtr.Zero"/> if the file path is null/empty, the file doesn't exist,
        /// or an error occurs during reading or allocation.
        /// </returns>
        /// <remarks>
        /// The caller is responsible for freeing the returned pointer using <see cref="Marshal.FreeHGlobal"/>
        /// when it's no longer needed to prevent memory leaks.
        /// </remarks>
        private static unsafe IntPtr ConvertFileToPointer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return IntPtr.Zero;
            }

            FileInfo fileInfo;
            try
            {
                fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    return IntPtr.Zero;
                }
            }
            catch (Exception) // Catch potential exceptions from FileInfo constructor (e.g., invalid path chars, security)
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
        /// Writes binary data from an unmanaged memory pointer to a new file.
        /// A unique filename is generated based on the current timestamp and format.
        /// </summary>
        /// <param name="dataPointer">An <see cref="IntPtr"/> pointing to the unmanaged binary data.</param>
        /// <param name="dataSize">The size (in bytes) of the data pointed to by <paramref name="dataPointer"/>.</param>
        /// <param name="format">The <see cref="ClipboardFormat"/> used to determine the file extension and subfolder.</param>
        /// <returns>The full path to the newly created file, or <see cref="string.Empty"/> if an error occurs.</returns>
        private static unsafe string ConvertPointerToFile(IntPtr dataPointer, UIntPtr dataSize, ClipboardFormat format)
        {
            if (dataPointer == IntPtr.Zero || dataSize == UIntPtr.Zero)
            {
                return string.Empty;
            }

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

        /// <summary>
        /// Writes bitmap data from an unmanaged memory pointer with BITMAP struct and the pixels to a new file.
        /// A unique filename is generated based on the current timestamp and format.
        /// </summary>
        /// <param name="dataPointer"> pointing to the unmanaged BITMAP struct and the pixels.</param>
        /// <returns>The full path to the newly created file, or <see cref="string.Empty"/> if an error occurs.</returns>
        private static unsafe string ConvertBitmapToFile(IntPtr dataPointer)
        {
            try
            {
                NativeHelper.BITMAP bitmapStruct = Marshal.PtrToStructure<NativeHelper.BITMAP>(dataPointer);
                IntPtr pData = dataPointer + Marshal.SizeOf<NativeHelper.BITMAP>();
                int stride = ((bitmapStruct.bmWidth * bitmapStruct.bmBitsPixel + 31) / 32) * 4;

                using Bitmap bitmap = new(
                    bitmapStruct.bmWidth,
                    bitmapStruct.bmHeight,
                    stride,
                    bitmapStruct.bmBitsPixel switch
                    {
                        1 => PixelFormat.Format1bppIndexed,
                        4 => PixelFormat.Format4bppIndexed,
                        8 => PixelFormat.Format8bppIndexed,
                        16 => PixelFormat.Format16bppRgb565,
                        24 => PixelFormat.Format24bppRgb,
                        32 => PixelFormat.Format32bppArgb,
                        _ => throw new NotSupportedException($"Bit depth {bitmapStruct.bmBitsPixel} is not supported.")
                    },
                    pData);

                string filePath = GenerateFilePath(ClipboardFormat.Bitmap);
                bitmap.Save(filePath, ImageFormat.Bmp);
                return filePath;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates a temporary Bitmap file from PNG data if available and required.
        /// </summary>
        /// <param name="dataModel">The data model with a path to PNG file.</param>
        /// <param name="tempBitmapPath">Output parameter for the path of the generated bitmap file.</param>
        /// <returns>True if a bitmap was generated, false otherwise.</returns>
        public static bool TryGenerateBitmapFromPng(DataModel dataModel, out string tempBitmapPath)
        {
            tempBitmapPath = string.Empty;

            if (dataModel.Format != ClipboardFormat.Png)
            {
                return false;
            }

            try
            {
                // Generate a unique temporary file path for the bitmap
                tempBitmapPath = Path.Combine(ApplicationData.GetDefault().TemporaryPath, string.Format(FILE_NAME_FORMAT_BITMAP, DateTime.Now));

                // Create a Bitmap from the PNG data and save it as a BMP file
                using Bitmap bitmap = new(dataModel.Data);
                bitmap.Save(tempBitmapPath, ImageFormat.Bmp);
                return true;
            }
            catch
            {
                tempBitmapPath = string.Empty; // Ensure path is empty on error
                return false;
            }
        }

        /// <summary>
        /// Generates a unique, absolute file path for storing external clipboard data based on the format.
        /// Ensures the target directory exists.
        /// </summary>
        /// <param name="format">The <see cref="ClipboardFormat"/> determining the subfolder and file extension.</param>
        /// <returns>A unique, absolute file path.</returns>
        /// <exception cref="NotImplementedException">Thrown if the format is not supported for file generation.</exception>
        private static string GenerateFilePath(ClipboardFormat format)
        {
            // Determine filename format string based on enum value
            string fileNameFormat = format switch
            {
                ClipboardFormat.Rtf => FILE_NAME_FORMAT_RTF,
                ClipboardFormat.Html => FILE_NAME_FORMAT_HTML,
                ClipboardFormat.Png => FILE_NAME_FORMAT_PNG,
                ClipboardFormat.Bitmap => FILE_NAME_FORMAT_BITMAP,
                _ => throw new NotImplementedException($"File path generation not implemented for format: {format}")
            };

            // Generate the unique filename using the current time
            string fileName = string.Format(fileNameFormat, DateTime.Now);

            // Construct the full path using the helper method (which handles subfolders)
            string fullPath = ConvertFileNameToFullPath(fileName, format);

            // Ensure the target directory exists before returning the path
            // Path.GetDirectoryName can return null if the path is invalid/root
            string? directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            else
            {
                throw new IOException($"Could not determine directory path for {fullPath}");
            }

            return fullPath;
        }
    }

    /// <summary>
    /// Represents the supported clipboard data formats within the application.
    /// </summary>
    public enum ClipboardFormat
    {
        [Description("CF_UNICODETEXT")]
        Text,
        [Description("CF_BITMAP")]
        Bitmap,
        [Description("Rich Text Format")]
        Rtf,
        [Description("HTML Format")]
        Html,
        [Description("PNG")]
        Png
    }

    /// <summary>
    /// Provides extension methods for working with Enums, particularly for retrieving descriptions.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the description string associated with an enum value via the <see cref="DescriptionAttribute"/>.
        /// If the attribute is not present, returns the enum value's name.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <returns>The description string or the enum's name.</returns>
        public static string GetDescription(this Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
                if (attribute != null)
                {
                    return attribute.Description;
                }
            }
            // If no attribute or field info, return the default enum name
            return value.ToString();
        }

        /// <summary>
        /// Gets the enum value of type <typeparamref name="T"/> that corresponds to the given description string.
        /// Compares against the <see cref="DescriptionAttribute"/> of each enum value.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="description">The description string to match.</param>
        /// <returns>The matching enum value, or the default value for the enum type (usually the first member or 0) if no match is found.</returns>
        public static T? FromDescription<T>(string description) where T : Enum
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                // Get the description of the current enum value
                // Compare it (case-sensitive) with the provided description
                if (value.GetDescription().Equals(description))
                {
                    return value;
                }
            }
            // If no match was found after checking all values, return the default value for the enum type
            return default;
        }
    }
}
