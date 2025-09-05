using Microsoft.Data.Sqlite;

namespace Rememory.Services.Migrations
{
    /// <summary>
    /// Creates FilesMetadata table
    /// App version 1.3.1
    /// </summary>
    public class FilesMetadataMigration : ISqliteMigration
    {
        public int Version => 4;

        public void Up(SqliteConnection connection)
        {
            using var upCommand = connection.CreateCommand();
            upCommand.CommandText = @"
            BEGIN TRANSACTION;

            CREATE TABLE IF NOT EXISTS FilesMetadata (
              Id INTEGER PRIMARY KEY NOT NULL,
              FilesCount INTEGER NOT NULL,
              FoldersCount INTEGER NOT NULL,
              FOREIGN KEY (Id) REFERENCES Data (Id) ON DELETE CASCADE
            );

            COMMIT;
            VACUUM;
            ";
            upCommand.ExecuteNonQuery();
        }

        public void Down(SqliteConnection connection)
        {
            using var downCommand = connection.CreateCommand();
            downCommand.CommandText = @"
            BEGIN TRANSACTION;

            DROP TABLE LinkMetadata;

            COMMIT;
            VACUUM;
            ";
            downCommand.ExecuteNonQuery();
        }
    }
}
