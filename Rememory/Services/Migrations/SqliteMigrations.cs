using Microsoft.Data.Sqlite;

namespace Rememory.Services.Migrations
{
    /// <summary>
    /// Creates ClipboardItems and LinksPreviewInfo tables.
    /// App version 1.0.0 - 1.2.0
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
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                IsFavorite      INTEGER NOT NULL,
                Time            TEXT    NOT NULL,
                OwnerPath       TEXT,
                OwnerIconBitmap BLOB,
                DataMap         TEXT    NOT NULL,
                HashMap         TEXT    NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LinksPreviewInfo (
                Id          INTEGER PRIMARY KEY,
                Title       TEXT,
                Description TEXT,
                ImageUrl    TEXT
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
}
