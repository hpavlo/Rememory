using Microsoft.Data.Sqlite;

namespace Rememory.Services.Migrations
{
    /// <summary>
    /// Creates two tables: Tags and ClipTags
    /// App version 1.3.0
    /// </summary>
    public class TagsMigration : ISqliteMigration
    {
        public int Version => 3;

        public void Up(SqliteConnection connection)
        {
            using var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = @"
            BEGIN TRANSACTION;

            CREATE TABLE IF NOT EXISTS Tags (
              Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
              Name TEXT NOT NULL,
              Color TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ClipTags (
              ClipId INTEGER NOT NULL,
              TagId INTEGER NOT NULL,
              PRIMARY KEY (ClipId, TagId),
              FOREIGN KEY (ClipId) REFERENCES Clips (Id) ON DELETE CASCADE,
              FOREIGN KEY (TagId) REFERENCES Tags (Id) ON DELETE CASCADE
            );

            COMMIT;
            ";
            clearCommand.ExecuteNonQuery();
        }

        public void Down(SqliteConnection connection)
        {
            using var clearCommand = connection.CreateCommand();
            clearCommand.CommandText = @"
            BEGIN TRANSACTION;

            DROP TABLE ClipTags;
            DROP TABLE Tags;

            COMMIT;
            ";
            clearCommand.ExecuteNonQuery();
        }
    }
}
