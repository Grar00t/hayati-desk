// Closes: D1, D2, D3, D4, D5
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace HayatiDesk.Data;

public sealed class DatabaseContext : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // D1: Concurrency control
    private bool _disposed;

    public string ConnectionString => _connectionString;

    public DatabaseContext(string databasePath)
    {
        // D2: Removed Cache=Shared
        // D3: Removed Pooling=true
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true // Set via connection string instead of PRAGMA
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // D4: Schema versioning
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA user_version;";
                var version = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                if (version < 1)
                {
                    await MigrateV1Async(connection);
                    await using var updateCmd = connection.CreateCommand();
                    updateCmd.CommandText = "PRAGMA user_version = 1;";
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            await using var pragmaWal = connection.CreateCommand();
            pragmaWal.CommandText = "PRAGMA journal_mode=WAL;";
            await pragmaWal.ExecuteNonQueryAsync();

            await using var pragmaSync = connection.CreateCommand();
            pragmaSync.CommandText = "PRAGMA synchronous=NORMAL;";
            await pragmaSync.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task MigrateV1Async(SqliteConnection connection)
    {
        const string createCategoriesTable = """
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                Color TEXT NOT NULL DEFAULT '#000000',
                CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
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
                CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
                UpdatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
            );
            """;

        const string createItemsIndex = """
            CREATE INDEX IF NOT EXISTS IX_Items_CategoryId ON Items(CategoryId);
            CREATE INDEX IF NOT EXISTS IX_Items_Completed ON Items(Completed);
            CREATE INDEX IF NOT EXISTS IX_Items_DueDate ON Items(DueDate);
            """;

        await using var tx = await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = createCategoriesTable + createItemsTable + createItemsIndex;
            await command.ExecuteNonQueryAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}
