using Microsoft.Extensions.DependencyInjection;
using Microsoft.Windows.Storage;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using RememoryCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Rememory.Services
{
    public class ClipTransferService : IClipTransferService
    {
        private readonly ClipboardMonitor _clipboardMonitor = App.Current.Services.GetService<ClipboardMonitor>()!;
        private readonly ITagService _tagService = App.Current.Services.GetService<ITagService>()!;
        private readonly IOwnerService _ownerService = App.Current.Services.GetService<IOwnerService>()!;
        private readonly IStorageService _storageService = App.Current.Services.GetService<IStorageService>()!;
        private readonly IClipboardService _clipboardService = App.Current.Services.GetService<IClipboardService>()!;

        public static readonly string BackupFileNameFormat_ = "Rememory_Backup_{0:yyyyMMdd_HHmmss}";
        public static readonly KeyValuePair<string, IList<string>> BackupFileType_ = new("Zip archive (*.zip)", [".zip"]);
        public static readonly IList<string> SupportedFormatFolders_ = [ FormatManager.RtfFolderName, FormatManager.HtmlFolderName, FormatManager.BitmapFolderName, FormatManager.PngFolderName ];

        public async Task<bool> ExportAsync(IList<ClipModel> clips, string destinationFilePath)
        {
            if (clips.Count == 0)
            {
                File.Delete(destinationFilePath);
                return false;
            }

            var backupDatabaseTempFilePath = GetTempDbFilePath();
            using SqliteService sqliteService = SqliteService.CreateForBackup(backupDatabaseTempFilePath);

            var dbExportResult = await Task.Run(() =>
            {
                try
                {
                    var owners = clips
                        .Select(clip => clip.Owner)
                        .Where(owner => owner is not null && owner.Id != 0)
                        .Cast<OwnerModel>()
                        .Distinct();

                    Dictionary<int, int> exportedOwnerIds = [];

                    foreach (var owner in owners)
                    {
                        var savedId = sqliteService.AddOwner(owner);
                        exportedOwnerIds.Add(owner.Id, savedId);
                    }

                    Dictionary<int, int> exportedClipIds = [];

                    foreach (var clip in clips)
                    {
                        int? ownerId = (clip.Owner?.Id is not null && exportedOwnerIds.TryGetValue(clip.Owner.Id, out var exportedOwnerId))
                            ? exportedOwnerId
                            : null;
                        var savedId = sqliteService.AddClip(clip, ownerId);
                        exportedClipIds.Add(clip.Id, savedId);
                    }

                    var clipsWithTags = clips
                        .Where(clip => clip.HasTags)
                        .ToArray();

                    var tags = clipsWithTags
                        .SelectMany(clip => clip.Tags)
                        .Distinct();

                    Dictionary<int, int> exportedTagIds = [];

                    foreach (var tag in tags)
                    {
                        var savedId = sqliteService.AddTag(tag);
                        exportedTagIds.Add(tag.Id, savedId);
                    }

                    var clipTags = clipsWithTags.SelectMany(clip => clip.Tags.Select(tag => (exportedClipIds[clip.Id], exportedTagIds[tag.Id])));
                    sqliteService.AddClipTags(clipTags);

                    return true;
                }
                catch
                {
                    return false;
                }
            });

            sqliteService.Dispose();

            if (!dbExportResult)
            {
                File.Delete(backupDatabaseTempFilePath);
                File.Delete(destinationFilePath);
                return false;
            }

            await using var zipStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            await archive.CreateEntryFromFileAsync(backupDatabaseTempFilePath, "ClipboardManager.db", CompressionLevel.Optimal);
            File.Delete(backupDatabaseTempFilePath);

            foreach (var clip in clips)
            {
                foreach (var dataModel in clip.Data.Values.Where(dm => dm.IsFile() && Path.Exists(dm.Data)))
                {
                    var parentName = Path.GetFileName(Path.GetDirectoryName(dataModel.Data))!;
                    var entryName = Path.Combine(parentName, Path.GetFileName(dataModel.Data));

                    await archive.CreateEntryFromFileAsync(dataModel.Data, entryName, CompressionLevel.Optimal);
                }
            }

            return true;
        }

        public async Task<bool> ImportAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            await using var zipStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, useAsync: true);
            await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var dbEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".db"));
            if (dbEntry is null)
            {
                return false;
            }

            // Data files extract
            
            var dataFilesToExtract = archive.Entries
                .Where(entry => SupportedFormatFolders_.Any(supportedFolderName => entry.FullName.StartsWith(supportedFolderName)))
                .Select(entry => new KeyValuePair<string, ZipArchiveEntry>(Path.Combine(_clipboardMonitor.HistoryFolderPath, entry.FullName), entry))
                .Where(dataFileEntryPair => !File.Exists(dataFileEntryPair.Key));

            foreach (var dataFileEntryPair in dataFilesToExtract)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dataFileEntryPair.Key)!);
                await dataFileEntryPair.Value.ExtractToFileAsync(dataFileEntryPair.Key);
            }

            // DB extract and import

            var extractedDbPath = GetTempDbFilePath();
            dbEntry.ExtractToFile(extractedDbPath);

            using SqliteService sqliteService = SqliteService.CreateForBackup(extractedDbPath);
            var clipsTimeHashSet = _clipboardService.Clips.Select(c => c.ClipTime).ToHashSet();   // To check if we already have this clip by cliptime

            IList<TagModel> tagsToImport = [];
            IList<ClipModel> clipsToImport = [];

            await Task.Run(() =>
            {
                var owners = sqliteService.GetOwners();
                tagsToImport = [.. sqliteService.GetTags()];
                clipsToImport = [.. sqliteService.GetClips(owners, tagsToImport).Where(clip => !clipsTimeHashSet.Contains(clip.ClipTime))];
            });

            Dictionary<int, TagModel> tagIdPairs = [];
            foreach (var tagToImport in tagsToImport)
            {
                if (_tagService.Tags.FirstOrDefault(tag => tagToImport.Name == tag.Name && tagToImport.ColorHex == tag.ColorHex) is not TagModel registeredTag)
                {
                    registeredTag = _tagService.RegisterTag(tagToImport.Name, tagToImport.ColorHex, tagToImport.IsCleaningEnabled);
                }
                tagIdPairs.Add(tagToImport.Id, registeredTag);
            }

            var clipOwnerIds = new List<(ClipModel OldClip, ClipModel NewClip, int? ownerId)>(clipsToImport.Count);
            foreach (var clip in clipsToImport)
            {
                var newClip = new ClipModel()
                {
                    ClipTime = clip.ClipTime,
                    IsFavorite = clip.IsFavorite,
                    Data = clip.Data,
                    IsLink = clip.IsLink
                };

                var owner = _ownerService.RegisterClipOwner(newClip, clip.Owner?.Path, clip.Owner?.Icon);
                owner.Name ??= clip.Owner?.Name;   // To update owner name, if it was not found

                clipOwnerIds.Add((clip, newClip, owner?.Id != 0 ? owner?.Id : null));
            }

            await Task.Run(() =>
            {
                foreach (var (_, newClip, ownerId) in clipOwnerIds)
                {
                    _storageService.AddClip(newClip, ownerId);
                }
            });

            IList<ClipModel> importedClips = [];
            foreach (var (oldClip, newClip, _) in clipOwnerIds)
            {
                var tagIds = oldClip.Tags.Select(tag => tag.Id).ToArray();
                foreach (var id in tagIds)
                {
                    _tagService.AddClipToTag(tagIdPairs[id], newClip);
                }

                importedClips.Add(newClip);
            }

            foreach (var insertedTag in tagIdPairs.Values.Where(tag => tag.ClipsCount == 0))
            {
                _tagService.UnregisterTag(insertedTag);
            }

            _clipboardService.InsertClips(importedClips);

            sqliteService.Dispose();
            File.Delete(extractedDbPath);

            return true;
        }

        private static string GetTempDbFilePath() => Path.Combine(ApplicationData.GetDefault().TemporaryFolder.Path, string.Format("Backup_{0:yyyyMMdd_HHmmss}.db", DateTime.Now));
    }
}
