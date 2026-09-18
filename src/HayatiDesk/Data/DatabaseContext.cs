using System.Data;
using Microsoft.Data.Sqlite;

namespace HayatiDesk.Data;

public sealed class DatabaseContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public DatabaseContext(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(connectionString);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        using var pragmaWal = _connection.CreateCommand();
        pragmaWal.CommandText = "PRAGMA journal_mode=WAL;";
        await pragmaWal.ExecuteNonQueryAsync();

        using var pragmaSync = _connection.CreateCommand();
        pragmaSync.CommandText = "PRAGMA synchronous=NORMAL;";
        await pragmaSync.ExecuteNonQueryAsync();

        using var pragmaForeignKeys = _connection.CreateCommand();
        pragmaForeignKeys.CommandText = "PRAGMA foreign_keys=ON;";
        await pragmaForeignKeys.ExecuteNonQueryAsync();

        await CreateTablesAsync();
    }

    private async Task CreateTablesAsync()
    {
        const string createCategoriesTable = """
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Color TEXT NOT NULL DEFAULT '#000000',
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            """;

        const string createItemsTable = """
            CREATE TABLE IF NOT EXISTS Items (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                CategoryId INTEGER NOT NULL,
                Priority INTEGER NOT NULL DEFAULT 0,
                DueDate TEXT,
                Completed INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
            );
            """;

        const string createItemsIndex = """
            CREATE INDEX IF NOT EXISTS IX_Items_CategoryId ON Items(CategoryId);
            CREATE INDEX IF NOT EXISTS IX_Items_Completed ON Items(Completed);
            CREATE INDEX IF NOT EXISTS IX_Items_DueDate ON Items(DueDate);
            """;

        await using var command = _connection.CreateCommand();
        command.CommandText = createCategoriesTable + createItemsTable + createItemsIndex;
        await command.ExecuteNonQueryAsync();
    }

    public SqliteConnection Connection => _connection;

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_connection.State == ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
            await _connection.DisposeAsync();
            _disposed = true;
        }
    }
}
