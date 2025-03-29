using Microsoft.Data.Sqlite;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Services.Migrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Rememory.Services
{
    public class SqliteService : IStorageService
    {
        private readonly string _connectionString = $"Data Source={Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, "ClipboardManager.db")}";
        private int _currentVersion;

        public SqliteService()
        {
            Directory.CreateDirectory(ClipboardFormatHelper.RootHistoryFolderPath);

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            ApplyMigrations(connection);
        }

        public int SaveClipboardItem(ClipboardItem item)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ClipboardItems (IsFavorite, Time, OwnerPath, OwnerIconBitmap, DataMap, HashMap)
                VALUES ($isFavorite, $time, $OwnerPath, $ownerIconBitmap, $dataMap, $hashMap)";

            command.Parameters.AddWithValue("$isFavorite", item.IsFavorite ? 1 : 0);
            command.Parameters.AddWithValue("$time", item.Time);
            command.Parameters.AddWithValue("$OwnerPath", item.OwnerPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$ownerIconBitmap", item.OwnerIconBitmap ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$dataMap", SerializeDataMap(item.DataMap));
            command.Parameters.AddWithValue("$hashMap", SerializeDictionary(item.HashMap));

            command.ExecuteNonQuery();

            // Retrieve the ID of the last inserted row
            var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid()";
            return Convert.ToInt32(idCommand.ExecuteScalar());
        }

        public void SaveLinkPreviewInfo(ClipboardLinkItem item)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO LinksPreviewInfo (Id, Title, Description, ImageUrl)
                VALUES ($id, $title, $description, $imageUrl)";

            command.Parameters.AddWithValue("$id", item.Id);
            command.Parameters.AddWithValue("$title", item.Title ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$description", item.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$imageUrl", item.Image?.UriSource?.OriginalString ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        public void UpdateClipboardItem(ClipboardItem item)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ClipboardItems
                SET IsFavorite = $isFavorite,
                    Time = $time,
                    OwnerPath = $OwnerPath,
                    OwnerIconBitmap = $ownerIconBitmap,
                    DataMap = $dataMap,
                    HashMap = $hashMap
                WHERE Id = $id";

            command.Parameters.AddWithValue("$id", item.Id);
            command.Parameters.AddWithValue("$isFavorite", item.IsFavorite ? 1 : 0);
            command.Parameters.AddWithValue("$time", item.Time);
            command.Parameters.AddWithValue("$OwnerPath", item.OwnerPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$ownerIconBitmap", item.OwnerIconBitmap ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$dataMap", SerializeDataMap(item.DataMap));
            command.Parameters.AddWithValue("$hashMap", SerializeDictionary(item.HashMap));

            command.ExecuteNonQuery();
        }

        public void DeleteClipboardItem(ClipboardItem item)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ClipboardItems WHERE Id = $id";
            command.Parameters.AddWithValue("$id", item.Id);

            command.ExecuteNonQuery();

            if (item is ClipboardLinkItem)
            {
                command.CommandText = "DELETE FROM LinksPreviewInfo WHERE Id = $id";
                command.ExecuteNonQuery();
            }
        }

        public void DeleteAllClipboardItems()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ClipboardItems";
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM LinksPreviewInfo";
            command.ExecuteNonQuery();
        }

        public List<ClipboardItem> LoadClipboardItems()
        {
            List<ClipboardItem> items = [];

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT items.Id, items.IsFavorite, items.Time, items.OwnerPath, items.OwnerIconBitmap, items.DataMap, items.HashMap,
                           linkInfo.Title, linkInfo.Description, linkInfo.ImageUrl 
                    FROM ClipboardItems items LEFT JOIN LinksPreviewInfo linkInfo ON items.Id = linkInfo.Id
                    ORDER BY items.Time DESC";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ClipboardItem item;

                    var id = reader.GetInt32(0);
                    var isFavorite = reader.GetInt32(1) > 0;
                    var time = reader.GetDateTime(2);
                    var ownerPath = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                    var ownerIconBitmap = reader.IsDBNull(4) ? null : (byte[])reader.GetValue(4);
                    var dataMap = DeserializeDataMap(reader.GetString(5));
                    var hashMap = DeserializeDictionary<ClipboardFormat, byte[]>(reader.GetString(6));

                    var title = reader.IsDBNull(7) ? null : reader.GetString(7);
                    var description = reader.IsDBNull(8) ? null : reader.GetString(8);
                    var imageUrl = reader.IsDBNull(9) ? null : reader.GetString(9);

                    if (string.IsNullOrEmpty(title) &&
                        string.IsNullOrEmpty(description) &&
                        string.IsNullOrEmpty(imageUrl))
                    {
                        item = new ClipboardItem
                        {
                            Id = id,
                            IsFavorite = isFavorite,
                            Time = time,
                            OwnerPath = ownerPath,
                            OwnerIconBitmap = ownerIconBitmap,
                            DataMap = dataMap,
                            HashMap = hashMap
                        };
                    }
                    else
                    {
                        item = new ClipboardLinkItem
                        {
                            Id = id,
                            IsFavorite = isFavorite,
                            Time = time,
                            OwnerPath = ownerPath,
                            OwnerIconBitmap = ownerIconBitmap,
                            DataMap = dataMap,
                            HashMap = hashMap,
                            Title = title,
                            Description = description,
                            HasInfoLoaded = true
                        };
                        try
                        {
                            ((ClipboardLinkItem)item).Image.UriSource = new Uri(imageUrl ?? string.Empty);
                        }
                        catch (UriFormatException) { }
                    }
                    
                    items.Add(item);
                }
            }

            return items;
        }

        #region DB migration

        /// <summary>
        /// Do database migration if current DB version is no the latest one
        /// </summary>
        /// <param name="connection">Connection to Sqlite database</param>
        private void ApplyMigrations(SqliteConnection connection)
        {
            _currentVersion = GetDatabaseVersion(connection);
            var migrations = GetMigrations();
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
            catch (SqliteException e)
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

        #region Serialisation

        // Serialize clipboard data map to JSON string
        // Convert file paths to file names only
        private string SerializeDataMap(Dictionary<ClipboardFormat, string> dataMap)
        {
            var updatedDataMap = dataMap.ToDictionary(
                pair => pair.Key,
                pair => pair.Key == ClipboardFormat.Text ? pair.Value : ClipboardFormatHelper.ConvertFullPathToFileName(pair.Value)
            );

            return SerializeDictionary(updatedDataMap);

        }

        // Deserialise JSON string to clipboard data map
        // Convert file names to file paths
        private Dictionary<ClipboardFormat, string> DeserializeDataMap(string jsonData)
        {
            var deserializedData = DeserializeDictionary<ClipboardFormat, string>(jsonData);

            return deserializedData.ToDictionary(
                pair => pair.Key,
                pair => pair.Key == ClipboardFormat.Text ? pair.Value : ClipboardFormatHelper.ConvertFileNameToFullPath(pair.Value, pair.Key)
            );
        }

        private string SerializeDictionary<T, U>(Dictionary<T, U> dict) where T : notnull
        {
            return JsonSerializer.Serialize(dict);
        }

        private Dictionary<T, U> DeserializeDictionary<T, U>(string jsonData) where T : notnull
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<T, U>>(jsonData) ?? [];
            }
            catch
            {
                return [];
            }
        }

        #endregion
    }
}
