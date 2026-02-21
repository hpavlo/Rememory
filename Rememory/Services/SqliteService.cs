using CommunityToolkit.WinUI.Helpers;
using Microsoft.Data.Sqlite;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Services.Migrations;
using RememoryCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rememory.Services
{
    /// <summary>
    /// SQlite implementation of <see cref="IStorageService"/>
    /// </summary>
    public class SqliteService : IStorageService, IDisposable
    {
        private readonly string _connectionString;
        private int _currentVersion;
        /// <summary>
        /// Optionally updates the model ID with the new DB primary key
        /// </summary>
        private bool _updateModelIds;
        /// <summary>
        /// Used for backup to avoid opening new connection each time
        /// </summary>
        private SqliteConnection? _cachedConnection;

        private SqliteService(string connectionString, bool updateModelIds, bool cacheConnections)
        {
            _connectionString = connectionString;
            _updateModelIds = updateModelIds;

            if (cacheConnections)
            {
                _cachedConnection = new SqliteConnection(connectionString);
                _cachedConnection.Open();
            }
        }

        public void Dispose()
        {
            (_cachedConnection as IDisposable)?.Dispose();
            _cachedConnection = null;
        }

        public static SqliteService CreateMain(string historyFolder)
        {
            Directory.CreateDirectory(historyFolder);
            var path = Path.Combine(historyFolder, "ClipboardManager.db");
            var service = new SqliteService($"Data Source={path}", true, false);
            service.InitializeDatabase();
            return service;
        }

        public static SqliteService CreateForBackup(string tempFilePath)
        {
            var service = new SqliteService($"Data Source={tempFilePath};Pooling=false", false, true);
            service.InitializeDatabase();
            return service;
        }

        #region Owners

        public IEnumerable<OwnerModel> GetOwners()
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              Id,
              Path,
              Name,
              Icon
            FROM
              Owners;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string path = reader.GetString(1);
                string? name = reader.IsDBNull(2) ? null : reader.GetString(2);
                byte[]? icon = reader.IsDBNull(3) ? null : (byte[])reader.GetValue(3);

                yield return new OwnerModel(path) { Id = id, Name = name, Icon = icon };
            }

            TryDisposeConnection(connection);
        }

        public int AddOwner(OwnerModel owner)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              Owners (Path, Name, Icon)
            VALUES
              (@path, @name, @icon);

            SELECT
              last_insert_rowid();
            ";

            command.Parameters.AddWithValue("path", owner.Path);
            command.Parameters.AddWithValue("name", owner.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("icon", owner.Icon ?? (object)DBNull.Value);

            var id = Convert.ToInt32(command.ExecuteScalar());

            TryDisposeConnection(connection);

            if (_updateModelIds)
            {
                owner.Id = id;
            }

            return id;
        }

        public void UpdateOwner(OwnerModel owner)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Owners
            SET
              Name = @name,
              Icon = @icon
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", owner.Id);
            command.Parameters.AddWithValue("name", owner.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("icon", owner.Icon ?? (object)DBNull.Value);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void DeleteOwner(int id)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Owners
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        #endregion

        #region Clips

        public IEnumerable<ClipModel> GetClips(IEnumerable<OwnerModel> owners, IEnumerable<TagModel> tags)
        {
            var connection = CreateOpenedConnection();

            Dictionary<int, OwnerModel> ownersDictionary = owners.ToDictionary(owner => owner.Id);
            Dictionary<int, TagModel> tagsDictionary = tags.ToDictionary(tag => tag.Id);
            Dictionary<int, List<int>> clipTagsDictionary = GetClipTags(connection)
                .GroupBy(pair => pair.Item1)
                .ToDictionary(group => group.Key, group => group.Select(pair => pair.Item2).ToList());

            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              Id,
              ClipTime,
              IsFavorite,
              OwnerId
            FROM
              Clips
            ORDER BY
              ClipTime DESC;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                DateTime clipTime = reader.GetDateTime(1);
                bool isFavorite = reader.GetBoolean(2);
                int? ownerId = reader.IsDBNull(3) ? null : reader.GetInt32(3);

                ClipModel clip = new()
                {
                    Id = id,
                    ClipTime = clipTime,
                    IsFavorite = isFavorite,
                    // Using 0 for the empty owner
                    Owner = ownersDictionary.TryGetValue(ownerId ?? 0, out var owner) ? owner : null,
                    Data = GetDataByClipId(id, connection).ToDictionary(d => d.Format),
                    Tags = clipTagsDictionary.TryGetValue(id, out var tagIds) ? [.. tagIds.Where(tagsDictionary.ContainsKey).Select(id => tagsDictionary[id])] : []
                };

                foreach (var tag in clip.Tags)
                {
                    tag.Clips.Add(clip);
                }

                if (clip.Owner is not null)
                {
                    clip.Owner.ClipsCount++;
                }

                if (clip.Data.TryGetValue(ClipboardFormat.Text, out var textData))
                {
                    clip.IsLink = Uri.TryCreate(textData.Data, UriKind.Absolute, out var uri)
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                }
                yield return clip;
            }

            TryDisposeConnection(connection);
        }

        public int AddClip(ClipModel clip, int? ownerId)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              Clips (ClipTime, IsFavorite, OwnerId)
            VALUES
              (@clipTime, @isFavorite, @ownerId);

            SELECT
              last_insert_rowid();
            ";

            command.Parameters.AddWithValue("clipTime", clip.ClipTime);
            command.Parameters.AddWithValue("isFavorite", clip.IsFavorite);
            command.Parameters.AddWithValue("ownerId", ownerId ?? (object)DBNull.Value);

            var id = Convert.ToInt32(command.ExecuteScalar());

            if (_updateModelIds)
            {
                clip.Id = id;
            }

            if (clip.Data is not null)
            {
                AddData(id, clip.Data.Values, connection);
            }

            TryDisposeConnection(connection);

            return id;
        }

        public void UpdateClip(ClipModel clip, int? ownerId)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Clips
            SET
              ClipTime = @clipTime,
              IsFavorite = @isFavorite,
              OwnerId = @ownerId
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", clip.Id);
            command.Parameters.AddWithValue("clipTime", clip.ClipTime);
            command.Parameters.AddWithValue("isFavorite", clip.IsFavorite);
            command.Parameters.AddWithValue("ownerId", ownerId ?? (object)DBNull.Value);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void DeleteClip(int id)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void DeleteOldClipsByTime(DateTime cutoffTime, bool deleteFavoriteClips)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips
            WHERE
              ClipTime < @cutoffTime
              AND (
                @deleteFavoriteClips
                OR NOT IsFavorite
              )
              AND Id NOT IN (
                SELECT
                  c.Id
                FROM
                  Clips c
                  JOIN ClipTags ct ON ct.ClipId = c.Id
                  JOIN Tags t ON t.Id = ct.TagId
                GROUP BY
                  c.Id
                HAVING
                  COUNT(*) != SUM(t.IsCleaningEnabled)
              );

            VACUUM;
            ";

            command.Parameters.AddWithValue("cutoffTime", cutoffTime);
            command.Parameters.AddWithValue("deleteFavoriteClips", deleteFavoriteClips);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void DeleteOldClipsByQuantity(int quantity, bool deleteFavoriteClips)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips
            WHERE
              (
                @deleteFavoriteClips
                OR NOT IsFavorite
              )
              AND Id NOT IN (
                SELECT
                  c.Id
                FROM
                  Clips c
                  JOIN ClipTags ct ON ct.ClipId = c.Id
                  JOIN Tags t ON t.Id = ct.TagId
                GROUP BY
                  c.Id
                HAVING
                  COUNT(*) != SUM(t.IsCleaningEnabled)
              )
              AND Id NOT IN (
                SELECT
                  Id
                FROM
                  Clips
                ORDER BY
                  ClipTime DESC
                LIMIT
                  @quantity
              );

            VACUUM;
            ";

            command.Parameters.AddWithValue("deleteFavoriteClips", deleteFavoriteClips);
            command.Parameters.AddWithValue("quantity", quantity);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void DeleteAllClips()
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips;
            DELETE FROM Owners;

            VACUUM;
            ";
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        #endregion

        #region Tags

        public IEnumerable<TagModel> GetTags()
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              Id,
              Name,
              Color,
              IsCleaningEnabled
            FROM
              Tags;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                string color = reader.GetString(2);
                bool isCleaningEnabled = reader.GetBoolean(3);

                yield return new TagModel(name, color, isCleaningEnabled) { Id = id };
            }

            TryDisposeConnection(connection);
        }

        public int AddTag(TagModel tag)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              Tags (Name, Color, IsCleaningEnabled)
            VALUES
              (@name, @color, @isCleaningEnabled);

            SELECT
              last_insert_rowid();
            ";

            command.Parameters.AddWithValue("name", tag.Name);
            command.Parameters.AddWithValue("color", tag.ColorHex);
            command.Parameters.AddWithValue("isCleaningEnabled", tag.IsCleaningEnabled);

            var id = Convert.ToInt32(command.ExecuteScalar());

            TryDisposeConnection(connection);

            if (_updateModelIds)
            {
                tag.Id = id;
            }

            return id;
        }

        public void UpdateTag(TagModel tag)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Tags
            SET
              Name = @name,
              Color = @color,
              IsCleaningEnabled = @isCleaningEnabled
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", tag.Id);
            command.Parameters.AddWithValue("name", tag.Name);
            command.Parameters.AddWithValue("color", tag.ColorHex);
            command.Parameters.AddWithValue("isCleaningEnabled", tag.IsCleaningEnabled);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void DeleteTag(int id)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Tags
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void AddClipTags(IEnumerable<(int ClipId, int TagId)> clipTags)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              ClipTags (ClipId, TagId)
            VALUES
              (@clipId, @tagId);
            ";

            var clipIdParameter = command.CreateParameter();
            clipIdParameter.ParameterName = "clipId";
            var tagIdParameter = command.CreateParameter();
            tagIdParameter.ParameterName = "tagId";
            command.Parameters.AddRange([clipIdParameter, tagIdParameter]);

            foreach (var (ClipId, TagId) in clipTags)
            {
                clipIdParameter.Value = ClipId;
                tagIdParameter.Value = TagId;
                command.ExecuteNonQuery();
            }

            TryDisposeConnection(connection);
        }

        public void DeleteClipTag(int clipId, int tagId)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM ClipTags
            WHERE
              ClipId = @clipId
              AND TagId = @tagId;
            ";

            command.Parameters.AddWithValue("clipId", clipId);
            command.Parameters.AddWithValue("tagId", tagId);
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        #endregion

        #region Metadata

        public void AddLinkMetadata(LinkMetadataModel linkMetadata, int dataId)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              LinkMetadata (Id, Url, Title, Description, Image)
            VALUES
              (@id, @url, @title, @description, @image);

            UPDATE Data
            SET
              MetadataFormat = @metadataFormat
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", dataId);
            command.Parameters.AddWithValue("url", linkMetadata.Url ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("title", linkMetadata.Title ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("description", linkMetadata.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("image", linkMetadata.Image ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("metadataFormat", linkMetadata.Format.GetDescription());
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void AddColorMetadata(ColorMetadataModel colorMetadata, int dataId)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            UPDATE Data
            SET
              MetadataFormat = @metadataFormat
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", dataId);
            command.Parameters.AddWithValue("metadataFormat", colorMetadata.Format.GetDescription());
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        public void AddFilesMetadata(FilesMetadataModel filesMetadata, int dataId)
        {
            var connection = CreateOpenedConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              FilesMetadata (Id, FilesCount, FoldersCount)
            VALUES
              (@id, @filesCount, @foldersCout);

            UPDATE Data
            SET
              MetadataFormat = @metadataFormat
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", dataId);
            command.Parameters.AddWithValue("filesCount", filesMetadata.FilesCount);
            command.Parameters.AddWithValue("foldersCout", filesMetadata.FoldersCount);
            command.Parameters.AddWithValue("metadataFormat", filesMetadata.Format.GetDescription());
            command.ExecuteNonQuery();

            TryDisposeConnection(connection);
        }

        #endregion

        public async Task<bool> ExportClipsAsync(IEnumerable<ClipModel> clips)
        {
            return await Task.Run(() =>
            {
                _cachedConnection = CreateOpenedConnection();

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
                        var savedId = AddOwner(owner);
                        exportedOwnerIds.Add(owner.Id, savedId);
                    }

                    Dictionary<int, int> exportedClipIds = [];

                    foreach (var clip in clips)
                    {
                        int? ownerId = (clip.Owner?.Id is not null && exportedOwnerIds.TryGetValue(clip.Owner.Id, out var exportedOwnerId))
                            ? exportedOwnerId
                            : null;
                        var savedId = AddClip(clip, ownerId);
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
                        var savedId = AddTag(tag);
                        exportedTagIds.Add(tag.Id, savedId);
                    }

                    var clipTags = clipsWithTags.SelectMany(clip => clip.Tags.Select(tag => (exportedClipIds[clip.Id], exportedTagIds[tag.Id])));
                    AddClipTags(clipTags);

                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    _cachedConnection.Dispose();
                }
            });
        }

        private IEnumerable<DataModel> GetDataByClipId(int clipId, SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              Id,
              Format,
              Data,
              Hash,
              MetadataFormat
            FROM
              Data
            WHERE
              ClipId = @clipId;
            ";

            command.Parameters.AddWithValue("clipId", clipId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                ClipboardFormat format = FormatManager.FormatFromName(reader.GetString(1));
                string data = reader.GetString(2);
                byte[] hash = (byte[])reader.GetValue(3);
                MetadataFormat? metadataFormat = reader.IsDBNull(4) ? null : EnumExtensions.FromDescription<MetadataFormat>(reader.GetString(4));

                IMetadata? metadataModel = metadataFormat switch
                {
                    MetadataFormat.Link => GetLinkMetadataById(id, connection),
                    MetadataFormat.Color => new ColorMetadataModel(),
                    MetadataFormat.Files => GetFilesMetadataById(id, data, connection),
                    _ => null
                };

                if (ClipboardFormatHelper.CanFormatBeFile(format))
                {
                    data = ClipboardFormatHelper.ConvertFileNameToFullPath(data, format);
                }

                yield return new DataModel(format, data, hash) { Id = id, Metadata = metadataModel };
            }
        }

        private void AddData(int clipId, IEnumerable<DataModel> dataCollection, SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              Data (ClipId, Format, Data, Hash)
            VALUES
              (@clipId, @format, @data, @hash);
            SELECT
              last_insert_rowid();
            ";
            var formatParameter = command.CreateParameter();
            formatParameter.ParameterName = "format";
            var dataParameter = command.CreateParameter();
            dataParameter.ParameterName = "data";
            var hashParameter = command.CreateParameter();
            hashParameter.ParameterName = "hash";
            command.Parameters.AddWithValue("clipId", clipId);
            command.Parameters.AddRange([formatParameter, dataParameter, hashParameter]);

            foreach (var data in dataCollection)
            {
                formatParameter.Value = FormatManager.FormatToName(data.Format);
                dataParameter.Value = data.IsFile() ? ClipboardFormatHelper.ConvertFullPathToFileName(data.Data) : data.Data;
                hashParameter.Value = data.Hash;

                var id = Convert.ToInt32(command.ExecuteScalar());

                if (_updateModelIds)
                {
                    data.Id = id;
                }

                switch (data.Metadata)
                {
                    case LinkMetadataModel linkMetadata:
                        AddLinkMetadata(linkMetadata, id);
                        break;
                    case ColorMetadataModel colorMetadata:
                        AddColorMetadata(colorMetadata, id);
                        break;
                    case FilesMetadataModel filesMetadata:
                        AddFilesMetadata(filesMetadata, id);
                        break;
                }
            }
        }

        private IEnumerable<(int, int)> GetClipTags(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              ClipId,
              TagId
            FROM
              ClipTags;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int clipId = reader.GetInt32(0);
                int tagId = reader.GetInt32(1);
                yield return (clipId, tagId);
            }
        }

        private LinkMetadataModel? GetLinkMetadataById(int id, SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              Url,
              Title,
              Description,
              Image
            FROM
              LinkMetadata
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                string? url = reader.IsDBNull(0) ? null : reader.GetString(0);
                string? title = reader.IsDBNull(1) ? null : reader.GetString(1);
                string? description = reader.IsDBNull(2) ? null : reader.GetString(2);
                string? image = reader.IsDBNull(3) ? null : reader.GetString(3);

                return new LinkMetadataModel()
                {
                    Url = url,
                    Title = title,
                    Description = description,
                    Image = image
                };
            }
            return null;
        }

        private FilesMetadataModel? GetFilesMetadataById(int id, string paths, SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              FilesCount,
              FoldersCount
            FROM
              FilesMetadata
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                int filesCount = reader.GetInt32(0);
                int foldersCount = reader.GetInt32(1);

                var metadata = new FilesMetadataModel()
                {
                    FilesCount = filesCount,
                    FoldersCount = foldersCount
                };
                metadata.SetPaths(paths);
                return metadata;
            }
            return null;
        }

        private void InitializeDatabase()
        {
            var connection = CreateOpenedConnection();
            ApplyMigrations(connection);

            TryDisposeConnection(connection);
        }

        private SqliteConnection CreateOpenedConnection()
        {
            if (_cachedConnection is not null)
            {
                return _cachedConnection;
            }

            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private void TryDisposeConnection(SqliteConnection connection)
        {
            if (connection != _cachedConnection)
            {
                connection.Dispose();
            }
        }

        #region DB migration

        /// <summary>
        /// Do database migration if current DB version is no the latest one
        /// </summary>
        private void ApplyMigrations(SqliteConnection connection)
        {
            _currentVersion = GetDatabaseVersion(connection);
            var migrations = GetMigrations();

            // If the app version is older than database
            if (migrations.Last().Version < _currentVersion)
            {
                NativeHelper.MessageBox(IntPtr.Zero,
                    "Current app version doesn't support this database!\nPlease update the app or reinstall it",
                    "Database error",
                    0x10);   // MB_ICONERROR | MB_OK

                Environment.Exit(1);
            }

            try
            {
                foreach (var migration in migrations)
                {
                    if (migration.Version > _currentVersion)
                    {
                        migration.Up(connection);
                        SetDatabaseVersion(connection, migration.Version);
                        _currentVersion = migration.Version;
                    }
                }
            }
            catch (Exception e)
            {
                using var command = connection.CreateCommand();
                command.CommandText = "ROLLBACK;";
                command.ExecuteNonQuery();

                int res = NativeHelper.MessageBox(IntPtr.Zero,
                    e.Message,
                    "Database migration failed",
                    0x15);   // MB_ICONERROR | MB_RETRYCANCEL

                if (res == 4)   // IDRETRY
                {
                    ApplyMigrations(connection);
                }
                else
                {
                    Environment.Exit(1);
                }
            }
        }

        /// <summary>
        /// Processes all classes that implement ISqliteMigration in the current assembly,
        /// creates instances of each migration class, sorts them by Version,
        /// and returns the sorted list
        /// </summary>
        /// <returns>The sorted list of ISqliteMigration objects</returns>
        private IEnumerable<ISqliteMigration> GetMigrations()
        {
            var assembly = GetType().Assembly;
            var migrations = assembly.GetTypes()
                .Where(type => typeof(ISqliteMigration).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .Select(type => (ISqliteMigration)Activator.CreateInstance(type)!)
                .OrderBy(migration => migration.Version);
            return migrations;
        }

        private static int GetDatabaseVersion(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version;";
            return (int)(long)(command.ExecuteScalar() ?? 0);
        }

        private static void SetDatabaseVersion(SqliteConnection connection, int version)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA user_version = {version};";
            command.ExecuteNonQuery();
        }

        #endregion
    }
}
