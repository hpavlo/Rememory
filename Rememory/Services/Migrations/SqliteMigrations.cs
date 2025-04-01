using Microsoft.Data.Sqlite;
using Rememory.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Rememory.Services.Migrations
{
    /// <summary>
    /// Creates ClipboardItems and LinksPreviewInfo tables.
    /// App version 1.0.0
    /// </summary>
    public class InitialCreate : ISqliteMigration
    {
        public int Version => 1;

        public void Up(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            BEGIN TRANSACTION;

            CREATE TABLE IF NOT EXISTS ClipboardItems (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              IsFavorite INTEGER NOT NULL,
              Time TEXT NOT NULL,
              OwnerPath TEXT,
              OwnerIconBitmap BLOB,
              DataMap TEXT NOT NULL,
              HashMap TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LinksPreviewInfo (
              Id INTEGER PRIMARY KEY,
              Title TEXT,
              Description TEXT,
              ImageUrl TEXT
            );

            COMMIT;
            ";
            command.ExecuteNonQuery();
        }

        public void Down(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            BEGIN TRANSACTION;

            DROP TABLE IF EXISTS ClipboardItems;
            DROP TABLE IF EXISTS LinksPreviewInfo;

            COMMIT;
            ";
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Creates Owners, Data, Tags tables
    /// Migrate ClipboardItems and LinksPreviewInfo to updated tables
    /// App version 1.2._
    /// </summary>
    public class SplitTablesMigration// : ISqliteMigration
    {
        public int Version => 2;

        public void Up(SqliteConnection connection)
        {
            using var createCommand = connection.CreateCommand();
            createCommand.CommandText = @"
            BEGIN TRANSACTION;

            CREATE TABLE IF NOT EXISTS Owners (
              Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
              Path TEXT UNIQUE NOT NULL,
              Name TEXT,
              Icon BLOB
            );

            CREATE TABLE IF NOT EXISTS Clips (
              Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
              ClipTime DATETIME NOT NULL,
              IsFavorite INTEGER NOT NULL,
              OwnerId INTEGER,
              FOREIGN KEY (OwnerId) REFERENCES Owners (Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS Data (
              Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
              ClipId INTEGER NOT NULL,
              Format TEXT NOT NULL,
              Data TEXT NOT NULL,
              Hash BLOB NOT NULL,
              FOREIGN KEY (ClipId) REFERENCES Clips (Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Tags (
              Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
              Name TEXT UNIQUE NOT NULL,
              Icon BLOB
            );

            CREATE TABLE IF NOT EXISTS ClipTags (
              ClipId INTEGER NOT NULL,
              TagId INTEGER NOT NULL,
              PRIMARY KEY (ClipId, TagId),
              FOREIGN KEY (ClipId) REFERENCES Clips (Id) ON DELETE CASCADE,
              FOREIGN KEY (TagId) REFERENCES Tags (Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS LinkMetadata (
              Id INTEGER PRIMARY KEY NOT NULL,
              Url TEXT NOT NULL,
              Title TEXT,
              Description TEXT,
              Image TEXT,
              FOREIGN KEY (Id) REFERENCES Data (Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Owners_Path ON Owners (Path);

            INSERT INTO
              Owners (Path, Icon)
            SELECT DISTINCT
              OwnerPath,
              OwnerIconBitmap
            FROM
              ClipboardItems
            WHERE
              OwnerPath IS NOT NULL
              AND OwnerPath NOT LIKE '';

            INSERT INTO
              Clips (Id, ClipTime, IsFavorite, OwnerId)
            SELECT
              c.Id,
              c.Time,
              c.IsFavorite,
              o.Id
            FROM
              ClipboardItems c
              LEFT JOIN Owners o ON o.Path = c.OwnerPath;
            ";
            createCommand.ExecuteNonQuery();

            PopulateOwnerNames(connection);
            MigrateUpDataWithLinks(connection);

            using var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = @"
            DROP TABLE ClipboardItems;
            DROP TABLE LinksPreviewInfo;

            COMMIT;
            VACUUM;
            ";
            clearCommand.ExecuteNonQuery();
        }

        public void Down(SqliteConnection connection)
        {
            using var downCommand = connection.CreateCommand();
            downCommand.CommandText = @"
            BEGIN TRANSACTION;

            CREATE TABLE IF NOT EXISTS ClipboardItems (
              Id INTEGER PRIMARY KEY AUTOINCREMENT,
              IsFavorite INTEGER NOT NULL,
              Time TEXT NOT NULL,
              OwnerPath TEXT,
              OwnerIconBitmap BLOB,
              DataMap TEXT NOT NULL,
              HashMap TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LinksPreviewInfo (
              Id INTEGER PRIMARY KEY,
              Title TEXT,
              Description TEXT,
              ImageUrl TEXT
            );

            INSERT INTO
              ClipboardItems (
                Id,
                IsFavorite,
                Time,
                OwnerPath,
                OwnerIconBitmap,
                DataMap,
                HashMap
              )
            SELECT
              c.Id,
              c.IsFavorite,
              c.ClipTime,
              o.Path,
              o.Icon,
              '',
              ''
            FROM
              Clips c
              LEFT JOIN Owners o ON o.Id = c.OwnerId;
            ";
            downCommand.ExecuteNonQuery();

            MigrateDownDataWithLinks(connection);

            using var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = @"
            DROP TABLE ClipTags;
            DROP TABLE Tags;
            DROP TABLE LinkMetadata;
            DROP TABLE Data;
            DROP TABLE Clips;
            DROP TABLE Owners;

            COMMIT;
            VACUUM;
            ";
            clearCommand.ExecuteNonQuery();
        }

        // Migration UP
        private void PopulateOwnerNames(SqliteConnection connection)
        {
            // Init owner names inject command
            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
            UPDATE Owners
            SET
              Name = @name
            WHERE
              Id = @id;
            ";
            var idParameter = updateCommand.CreateParameter();
            idParameter.ParameterName = "id";
            var nameParameter = updateCommand.CreateParameter();
            nameParameter.ParameterName = "name";
            updateCommand.Parameters.AddRange([idParameter, nameParameter]);

            // Init select command
            using var readCommand = connection.CreateCommand();
            readCommand.CommandText = @"
            SELECT
              Id,
              Path
            FROM
              Owners;
            ";
            using var reader = readCommand.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string path = reader.GetString(1);

                if (File.Exists(path)
                    && FileVersionInfo.GetVersionInfo(path).ProductName is string ownerName
                    && !string.IsNullOrWhiteSpace(ownerName))
                {
                    idParameter.Value = id;
                    nameParameter.Value = ownerName;
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        // Migration UP
        private void MigrateUpDataWithLinks(SqliteConnection connection)
        {
            // Init data insert command
            var dataInsertCommand = connection.CreateCommand();
            dataInsertCommand.CommandText = @"
            INSERT INTO
              Data (ClipId, Format, Data, Hash)
            VALUES
              (@clipId, @format, @data, @hash);
            SELECT
              last_insert_rowid();
            ";
            var clipIdParameter = dataInsertCommand.CreateParameter();
            clipIdParameter.ParameterName = "clipId";
            var formatParameter = dataInsertCommand.CreateParameter();
            formatParameter.ParameterName = "format";
            var dataParameter = dataInsertCommand.CreateParameter();
            dataParameter.ParameterName = "data";
            var hashParameter = dataInsertCommand.CreateParameter();
            hashParameter.ParameterName = "hash";
            dataInsertCommand.Parameters.AddRange([clipIdParameter, formatParameter, dataParameter, hashParameter]);

            // Init link metadata insert command
            var linkMetadataInsertCommand = connection.CreateCommand();
            linkMetadataInsertCommand.CommandText = @"
            INSERT INTO
              LinkMetadata (Id, Url, Title, Description, Image)
            VALUES
              (@id, @url, @title, @description, @image);
            ";
            var idParameter = linkMetadataInsertCommand.CreateParameter();
            idParameter.ParameterName = "id";
            var urlParameter = linkMetadataInsertCommand.CreateParameter();
            urlParameter.ParameterName = "url";
            var titleParameter = linkMetadataInsertCommand.CreateParameter();
            titleParameter.ParameterName = "title";
            var descriptionParameter = linkMetadataInsertCommand.CreateParameter();
            descriptionParameter.ParameterName = "description";
            var imageParameter = linkMetadataInsertCommand.CreateParameter();
            imageParameter.ParameterName = "image";
            linkMetadataInsertCommand.Parameters.AddRange([idParameter, urlParameter, titleParameter, descriptionParameter, imageParameter]);

            // Init select command
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
            SELECT
              c.Id,
              c.DataMap,
              c.HashMap,
              l.Title,
              l.Description,
              l.ImageUrl
            FROM
              ClipboardItems c
              LEFT JOIN LinksPreviewInfo l ON c.Id = l.Id;
            ";
            using var reader = selectCommand.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                var dataMap = DeserializeDataMap(reader.GetString(1));
                var hashMap = DeserializeDictionary<ClipboardFormat, byte[]>(reader.GetString(2));
                string? title = reader.IsDBNull(3) ? null : reader.GetString(3);
                string? description = reader.IsDBNull(4) ? null : reader.GetString(4);
                string? imageUrl = reader.IsDBNull(5) ? null : reader.GetString(5);
                bool hasLinkMetadata = !(string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description) && string.IsNullOrEmpty(imageUrl));

                clipIdParameter.Value = id;

                foreach (var dataItem in dataMap)
                {
                    if (!hashMap.ContainsKey(dataItem.Key))
                    {
                        continue;
                    }

                    formatParameter.Value = dataItem.Key.GetDescription();
                    dataParameter.Value = dataItem.Value;
                    hashParameter.Value = hashMap[dataItem.Key];

                    long newId = (long)dataInsertCommand.ExecuteScalar()!;

                    if (hasLinkMetadata && dataItem.Key == ClipboardFormat.Text)
                    {
                        idParameter.Value = newId;
                        urlParameter.Value = dataItem.Value;
                        titleParameter.Value = title ?? (object)DBNull.Value;
                        descriptionParameter.Value = description ?? (object)DBNull.Value;
                        imageParameter.Value = imageUrl ?? (object)DBNull.Value;
                        linkMetadataInsertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        // Migration DOWN
        private void MigrateDownDataWithLinks(SqliteConnection connection)
        {
            // Init data and hash inject command
            var dataInsertCommand = connection.CreateCommand();
            dataInsertCommand.CommandText = @"
            UPDATE ClipboardItems
            SET
              DataMap = @data,
              HashMap = @hash
            WHERE
              Id = @id;
            ";
            var clipIdParameter = dataInsertCommand.CreateParameter();
            clipIdParameter.ParameterName = "id";
            var dataParameter = dataInsertCommand.CreateParameter();
            dataParameter.ParameterName = "data";
            var hashParameter = dataInsertCommand.CreateParameter();
            hashParameter.ParameterName = "hash";
            dataInsertCommand.Parameters.AddRange([clipIdParameter, dataParameter, hashParameter]);

            // Init link oreview info inject command
            var linkInsertCommand = connection.CreateCommand();
            linkInsertCommand.CommandText = @"
            INSERT INTO
              LinksPreviewInfo (Id, Title, Description, ImageUrl)
            VALUES
              (@id, @title, @description, @image);
            ";
            var idParameter = linkInsertCommand.CreateParameter();
            idParameter.ParameterName = "id";
            var titleParameter = linkInsertCommand.CreateParameter();
            titleParameter.ParameterName = "title";
            var descriptionParameter = linkInsertCommand.CreateParameter();
            descriptionParameter.ParameterName = "description";
            var imageParameter = linkInsertCommand.CreateParameter();
            imageParameter.ParameterName = "image";
            linkInsertCommand.Parameters.AddRange([idParameter, titleParameter, descriptionParameter, imageParameter]);

            // Init select command
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
            SELECT
              d.ClipId,
              d.Format,
              d.Data,
              d.Hash,
              l.Title,
              l.Description,
              l.Image
            FROM
              Data d
              LEFT JOIN LinkMetadata l ON d.Id = l.Id
            ORDER BY
              d.ClipId;
            ";
            using var dataReader = selectCommand.ExecuteReader();

            // Temp variables to collect multiple data per clip
            int lastClipId = -1;
            Dictionary<ClipboardFormat, string> dataTemp = [];
            Dictionary<ClipboardFormat, byte[]> hashTemp = [];

            while (dataReader.Read())
            {
                int id = dataReader.GetInt32(0);
                ClipboardFormat? format = EnumExtensions.FromDescription<ClipboardFormat>(dataReader.GetString(1));
                string data = dataReader.GetString(2);
                byte[] hash = (byte[])dataReader.GetValue(3);

                string? title = dataReader.IsDBNull(4) ? null : dataReader.GetString(4);
                string? description = dataReader.IsDBNull(5) ? null : dataReader.GetString(5);
                string? image = dataReader.IsDBNull(6) ? null : dataReader.GetString(6);

                bool hasLinkMetadata = !(string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description) && string.IsNullOrEmpty(image));

                if (hasLinkMetadata)
                {
                    idParameter.Value = id;
                    titleParameter.Value = title ?? (object)DBNull.Value;
                    descriptionParameter.Value = description ?? (object)DBNull.Value;
                    imageParameter.Value = image ?? (object)DBNull.Value;
                    linkInsertCommand.ExecuteNonQuery();
                }

                // We have multiple data per clip
                // Check if clip id was changed before inject data and hash
                if (lastClipId != id && dataTemp.Count > 0 && hashTemp.Count > 0)
                {
                    DataInsertExecute();
                }

                lastClipId = id;
                dataTemp.Add(format!.Value, data);
                hashTemp.Add(format!.Value, hash);
            }

            if (lastClipId > 0 && dataTemp.Count > 0 && hashTemp.Count > 0)
            {
                DataInsertExecute();
            }

            void DataInsertExecute()
            {
                clipIdParameter.Value = lastClipId;
                dataParameter.Value = SerializeDictionary(dataTemp);
                hashParameter.Value = SerializeDictionary(hashTemp);
                dataInsertCommand.ExecuteNonQuery();

                dataTemp.Clear();
                hashTemp.Clear();
            }
        }

        // Deserialise data map with file name only instead of full path
        private Dictionary<ClipboardFormat, string> DeserializeDataMap(string jsonData)
        {
            var deserializedData = DeserializeDictionary<ClipboardFormat, string>(jsonData);

            return deserializedData.ToDictionary(
                pair => pair.Key,
                pair => pair.Key == ClipboardFormat.Text ? pair.Value : ClipboardFormatHelper.ConvertFullPathToFileName(pair.Value)
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
