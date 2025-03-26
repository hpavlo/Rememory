using Microsoft.Data.Sqlite;
using Microsoft.Windows.Storage;
using Rememory.Contracts;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Rememory.Services
{
    public class SqliteService : IStorageService
    {
        private string _dbPath = Path.Combine(ApplicationData.GetDefault().LocalPath, "History", "ClipboardManager.db");
        private string _connectionString;
        private string _createMainTableQuery = @"
            CREATE TABLE IF NOT EXISTS ClipboardItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                IsFavorite INTEGER NOT NULL,
                Time TEXT NOT NULL,
                OwnerPath TEXT,
                OwnerIconBitmap BLOB,
                DataMap TEXT NOT NULL,
                HashMap TEXT NOT NULL
            )";
        private string _createLinksPreviewTableQuery = @"
            CREATE TABLE IF NOT EXISTS LinksPreviewInfo (
                Id INTEGER PRIMARY KEY,
                Title TEXT,
                Description TEXT,
                ImageUrl TEXT
            )";

        public SqliteService()
        {
            var dirPath = Path.GetDirectoryName(_dbPath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            _connectionString = $"Data Source={_dbPath}";

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = _createMainTableQuery;
            command.ExecuteNonQuery();

            command.CommandText = _createLinksPreviewTableQuery;
            command.ExecuteNonQuery();
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
                    SELECT items.*, linkInfo.Title, linkInfo.Description, linkInfo.ImageUrl 
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
    }
}
