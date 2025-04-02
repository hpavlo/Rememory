using Microsoft.Data.Sqlite;
using Rememory.Helper;
using Rememory.Models.NewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Rememory.Services.NewServices
{
    public class NewSqliteService
    {
        private readonly string _connectionString = $"Data Source={Path.Combine(ClipboardFormatHelper.RootHistoryFolderPath, "ClipboardManager.db")}";


        // move to OwnerService
        private Dictionary<int, OwnerModel> _owners = [];

        // test method
        public IEnumerable<ClipModel> ReadClips()
        {
            _owners = GetOwners().ToDictionary(o => o.Id);

            return GetClips();
        }

        public IEnumerable<OwnerModel> GetOwners()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
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
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
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
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
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
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Owners
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            command.ExecuteNonQuery();
        }


        public IEnumerable<ClipModel> GetClips()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT
              Id,
              ClipTime,
              IsFavorite,
              OwnerId
            FROM
              Clips;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                DateTime clipTime = reader.GetDateTime(1);
                bool isFavorite = reader.GetBoolean(2);
                int? ownerId = reader.IsDBNull(3) ? null : reader.GetInt32(3);

                yield return new ClipModel()
                {
                    Id = id,
                    ClipTime = clipTime,
                    IsFavorite = isFavorite,
                    Owner = ownerId is not null && _owners.TryGetValue((int)ownerId, out var owner) ? owner : null,
                    Data = GetDataByClipId(id, connection).ToDictionary(d => d.Format)
                };
            }
        }

        public void AddClip(ClipModel clip)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
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
            command.Parameters.AddWithValue("ownerId", clip.Owner?.Id ?? (object)DBNull.Value);
            clip.Id = Convert.ToInt32(command.ExecuteScalar());

            if (clip.Data is not null)
            {
                AddData(clip.Id, clip.Data.Values, connection);
            }
        }

        public void UpdateClip(ClipModel clip)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
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
            command.Parameters.AddWithValue("ownerId", clip.Owner?.Id ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        public void DeleteClip(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            DELETE FROM Clips
            WHERE
              Id = @id;
            ";

            command.Parameters.AddWithValue("id", id);
            command.ExecuteNonQuery();
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
                ClipboardFormat format = EnumExtensions.FromDescription<ClipboardFormat>(reader.GetString(1));
                string data = reader.GetString(2);
                byte[] hash = (byte[])reader.GetValue(3);
                MetadataFormat? metadataFormat = reader.IsDBNull(4) ? null : EnumExtensions.FromDescription<MetadataFormat>(reader.GetString(4));

                if (metadataFormat is null)
                {
                    yield return new DataModel(format, data, hash) { Id = id };
                }
                else if (metadataFormat == MetadataFormat.Link)
                {
                    var linkMetadata = GetLinkMetadataById(id, connection);
                    yield return new LinkMetadataModel(format, data, hash)
                    {
                        Id = id,
                        Url = linkMetadata.Item1,
                        Title = linkMetadata.Item2,
                        Description = linkMetadata.Item3,
                        Image = linkMetadata.Item4
                    };
                }
            }
        }

        private void AddData(int clipId, ICollection<DataModel> dataCollection, SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              Data (ClipId, Format, Data, Hash, MetadataFormat)
            VALUES
              (@clipId, @format, @data, @hash, @metadataFormat);
            SELECT
              last_insert_rowid();
            ";
            var formatParameter = command.CreateParameter();
            formatParameter.ParameterName = "format";
            var dataParameter = command.CreateParameter();
            dataParameter.ParameterName = "data";
            var hashParameter = command.CreateParameter();
            hashParameter.ParameterName = "hash";
            var metadataFormatParameter = command.CreateParameter();
            metadataFormatParameter.ParameterName = "metadataFormat";
            command.Parameters.AddWithValue("clipId", clipId);
            command.Parameters.AddRange([formatParameter, dataParameter, hashParameter, metadataFormatParameter]);

            foreach (var data in dataCollection)
            {
                formatParameter.Value = data.Format.GetDescription();
                dataParameter.Value = data.Data;
                hashParameter.Value = data.Hash;
                MetadataFormat? metadataFormat = data switch
                {
                    LinkMetadataModel => MetadataFormat.Link,
                    _ => null
                };
                metadataFormatParameter.Value = metadataFormat ?? (object)DBNull.Value;

                data.Id = Convert.ToInt32(command.ExecuteScalar());

                switch (metadataFormat)
                {
                    case MetadataFormat.Link:
                        AddLinkMetadata((LinkMetadataModel)data, connection);
                        break;
                }
            }
        }

        private (string?, string?, string?, string?) GetLinkMetadataById(int id, SqliteConnection connection)
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

                return (url, title, description, image);
            }
            return (null, null, null, null);
        }

        private void AddLinkMetadata(LinkMetadataModel linkMetadata, SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO
              LinkMetadata (Id, Url, Title, Description, Image)
            VALUES
              (@id, @url, @title, @description, @image);
            ";

            command.Parameters.AddWithValue("id", linkMetadata.Id);
            command.Parameters.AddWithValue("url", linkMetadata.Url ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("title", linkMetadata.Title ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("description", linkMetadata.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("image", linkMetadata.Image ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        private enum MetadataFormat
        {
            [Description("Link")]
            Link
        }
    }
}
