using Microsoft.Data.Sqlite;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.Metadata;
using Rememory.Services.Migrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rememory.Services
{
    /// <summary>
    /// SQlite implementation of <see cref="IStorageService"/>
    /// </summary>
    public class SqliteService : IStorageService
    {
        private readonly string _connectionString = $"Data Source={Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, "ClipboardManager.db")}";
        private int _currentVersion;

        public SqliteService()
        {
            Directory.CreateDirectory(ClipboardFormatHelper.RootHistoryFolderPath);
            using var connection = CreateAndOpenConnection();
            ApplyMigrations(connection);
        }

        #region Owners

        public IEnumerable<OwnerModel> GetOwners()
        {
            using var connection = CreateAndOpenConnection();
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
        }

        public void AddOwner(OwnerModel owner)
        {
            using var connection = CreateAndOpenConnection();
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
            owner.Id = Convert.ToInt32(command.ExecuteScalar());
        }

        public void UpdateOwner(OwnerModel owner)
        {
            using var connection = CreateAndOpenConnection();
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
        }

        public void DeleteOwner(int id)
        {
            using var connection = CreateAndOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Owners
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            command.ExecuteNonQuery();
        }

        #endregion

        #region Clips

        public IEnumerable<ClipModel> GetClips(Dictionary<int, OwnerModel> owners)
        {
            using var connection = CreateAndOpenConnection();
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
                    Owner = owners.TryGetValue(ownerId ?? 0, out var owner) ? owner : null,
                    Data = GetDataByClipId(id, connection).ToDictionary(d => d.Format)
                };

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
        }

        public void AddClip(ClipModel clip)
        {
            using var connection = CreateAndOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              Clips (ClipTime, IsFavorite, OwnerId)
            VALUES
              (@clipTime, @isFavorite, @ownerId);

            SELECT
              last_insert_rowid();
            ";

            // Don't save empty owner id
            int? ownerId = clip.Owner?.Id != 0 ? clip.Owner?.Id : null;

            command.Parameters.AddWithValue("clipTime", clip.ClipTime);
            command.Parameters.AddWithValue("isFavorite", clip.IsFavorite);
            command.Parameters.AddWithValue("ownerId", ownerId ?? (object)DBNull.Value);
            clip.Id = Convert.ToInt32(command.ExecuteScalar());

            if (clip.Data is not null)
            {
                AddData(clip.Id, clip.Data.Values, connection);
            }
        }

        public void UpdateClip(ClipModel clip)
        {
            using var connection = CreateAndOpenConnection();
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

            // Don't save empty owner id
            int? ownerId = clip.Owner?.Id != 0 ? clip.Owner?.Id : null;

            command.Parameters.AddWithValue("id", clip.Id);
            command.Parameters.AddWithValue("clipTime", clip.ClipTime);
            command.Parameters.AddWithValue("isFavorite", clip.IsFavorite);
            command.Parameters.AddWithValue("ownerId", ownerId ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        public void DeleteClip(int id)
        {
            using var connection = CreateAndOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            command.ExecuteNonQuery();
        }

        public void DeleteOldClipsByTime(DateTime cutoffTime, bool deleteFavoriteClips)
        {
            using var connection = CreateAndOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips
            WHERE
              ClipTime < @cutoffTime
              AND (
                @deleteFavoriteClips
                OR NOT IsFavorite
              );

            VACUUM;
            ";

            command.Parameters.AddWithValue("cutoffTime", cutoffTime);
            command.Parameters.AddWithValue("deleteFavoriteClips", deleteFavoriteClips);
            command.ExecuteNonQuery();
        }

        public void DeleteOldClipsByQuantity(int quantity)
        {
            using var connection = CreateAndOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips
            WHERE
              Id NOT IN (
                SELECT
                  Id
                FROM
                  Clips
                ORDER BY
                  ClipTime DESC
                LIMIT
                  @quantity
              );
            ";

            command.Parameters.AddWithValue("quantity", quantity);
            command.ExecuteNonQuery();
        }

        public void DeleteAllClips()
        {
            using var connection = CreateAndOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips;
            DELETE FROM Owners;

            VACUUM;
            ";
            command.ExecuteNonQuery();
        }

        #endregion

        #region Metadata

        public void AddLinkMetadata(LinkMetadataModel linkMetadata, int dataId)
        {
            using var connection = CreateAndOpenConnection();
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
            command.Parameters.AddWithValue("metadataFormat", MetadataFormat.Link.GetDescription());
            command.ExecuteNonQuery();
        }

        #endregion

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
                ClipboardFormat format = EnumExtensions.FromDescription<ClipboardFormat>(reader.GetString(1));
                string data = reader.GetString(2);
                byte[] hash = (byte[])reader.GetValue(3);
                MetadataFormat? metadataFormat = reader.IsDBNull(4) ? null : EnumExtensions.FromDescription<MetadataFormat>(reader.GetString(4));

                IMetadata? metadataModel = metadataFormat switch
                {
                    MetadataFormat.Link => GetLinkMetadataById(id, connection),
                    _ => null
                };

                if (ClipboardFormatHelper.CanFormatBeFile(format))
                {
                    data = ClipboardFormatHelper.ConvertFileNameToFullPath(data, format);
                }

                yield return new DataModel(format, data, hash) { Id = id, Metadata = metadataModel };
            }
        }

        private void AddData(int clipId, ICollection<DataModel> dataCollection, SqliteConnection connection)
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
                formatParameter.Value = data.Format.GetDescription();
                dataParameter.Value = data.IsFile() ? ClipboardFormatHelper.ConvertFullPathToFileName(data.Data) : data.Data;
                hashParameter.Value = data.Hash;

                data.Id = Convert.ToInt32(command.ExecuteScalar());
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

        private SqliteConnection CreateAndOpenConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
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

                App.Current.Exit();
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
                    App.Current.Exit();
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

        private int GetDatabaseVersion(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version;";
            return (int)(long)(command.ExecuteScalar() ?? 0);
        }

        private void SetDatabaseVersion(SqliteConnection connection, int version)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA user_version = {version};";
            command.ExecuteNonQuery();
        }

        #endregion
    }
}
