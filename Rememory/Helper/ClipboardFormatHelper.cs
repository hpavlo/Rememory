using Microsoft.Extensions.DependencyInjection;
using Rememory.Models;
using RememoryCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rememory.Helper
{
    /// <summary>
    /// Provides constants, mappings, and utility functions for handling different clipboard formats,
    /// managing associated file storage, and converting between managed types and unmanaged clipboard data.
    /// </summary>
    public static class ClipboardFormatHelper
    {
        private static readonly ClipboardMonitor _clipboardMonitor = App.Current.Services.GetService<ClipboardMonitor>()!;

        /// <summary>
        /// The base date/time format string used for generating unique filenames for stored clipboard data files.
        /// Format: YearMonthDay_HourMinuteSecondMillisecond (e.g., 20231027_153005123).
        /// </summary>
        public const string FileNameFormat_ = "{0:yyyyMMdd_HHmmssfff}";

        /// <summary>
        /// Provides a mapping between <see cref="ClipboardFormat"/> values and their
        /// corresponding file type filter for use in Save As dialogs.
        /// </summary>
        public static readonly Dictionary<ClipboardFormat, KeyValuePair<string, IList<string>>> SaveAsFormatFilters = new()
        {
            { ClipboardFormat.Text, new("Text file (*.txt)", [".txt"]) },
            { ClipboardFormat.Bitmap, new("Bitmap image (*.bmp)", [".bmp"]) },
            { ClipboardFormat.Rtf, new("Rich Text Format (*.rtf)", [".rtf"]) },
            { ClipboardFormat.Html, new("HTML file (*.htm;*.html)", [".htm", ".html"]) },
            { ClipboardFormat.Png, new("PNG image (*.png)", [".png"]) }
        };

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

            // Return true if PNG matches
            if (pngMatch) return true;

            // Check if both have Files data and their hashes match
            bool filesMatch = firstModel.Data.TryGetValue(ClipboardFormat.Files, out var firstFilesData)
                         && secondModel.Data.TryGetValue(ClipboardFormat.Files, out var secondFilesData)
                         && StructuralComparisons.StructuralEqualityComparer.Equals(firstFilesData?.Hash, secondFilesData?.Hash);

            // Return true if Files matches
            return filesMatch;
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
        /// Attempts to delete all known external data format folders (RTF, HTML, PNG, Bitmap)
        /// </summary>
        public static void ClearAllExternalData()
        {
            var historyFolder = _clipboardMonitor.HistoryFolderPath;

            try
            {
                DeleteFolder(Path.Combine(historyFolder, FormatManager.RtfFolderName));
                DeleteFolder(Path.Combine(historyFolder, FormatManager.HtmlFolderName));
                DeleteFolder(Path.Combine(historyFolder, FormatManager.PngFolderName));
                DeleteFolder(Path.Combine(historyFolder, FormatManager.BitmapFolderName));
            }
            catch { }

            static void DeleteFolder(string path)
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
            return format != ClipboardFormat.Text && format != ClipboardFormat.Files;
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
                ClipboardFormat.Rtf => FormatManager.RtfFolderName,
                ClipboardFormat.Html => FormatManager.HtmlFolderName,
                ClipboardFormat.Png => FormatManager.PngFolderName,
                ClipboardFormat.Bitmap => FormatManager.BitmapFolderName,
                // Explicitly handle Text or throw for unsupported formats intended for file storage.
                ClipboardFormat.Text => throw new ArgumentException("Text format should not be stored as an external file.", nameof(format)),
                _ => throw new NotImplementedException($"Folder mapping not implemented for format: {format}")
            };

            return Path.Combine(_clipboardMonitor.HistoryFolderPath, formatFolderName, fileName);
        }
    }
}
