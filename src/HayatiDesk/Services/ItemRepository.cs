// Closes: D1, D7
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HayatiDesk.Data;
using Microsoft.Data.Sqlite;

namespace HayatiDesk.Services;

public interface IItemRepository
{
    IAsyncEnumerable<Category> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<Item> GetItemsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Item> GetAllItemsAsync(CancellationToken cancellationToken = default);
    Task<int> AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<int> AddItemAsync(Item item, CancellationToken cancellationToken = default);
    Task<bool> UpdateItemAsync(Item item, CancellationToken cancellationToken = default);
    Task<bool> DeleteItemAsync(int itemId, CancellationToken cancellationToken = default);
    Task<int> GetCompletedCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}

public sealed class ItemRepository : IItemRepository
{
    private readonly string _connectionString;

    public ItemRepository(DatabaseContext databaseContext)
    {
        _connectionString = databaseContext.ConnectionString;
    }

    // D7: Added [EnumeratorCancellation]
    public async IAsyncEnumerable<Category> GetAllCategoriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, Name, Color, CreatedAt FROM Categories ORDER BY Name;";
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Color = reader.GetString(2),
                CreatedAt = reader.GetString(3)
            };
        }
    }

    // D7: Added [EnumeratorCancellation]
    public async IAsyncEnumerable<Item> GetItemsByCategoryAsync(int categoryId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Title, Description, CategoryId, Priority, DueDate, Completed, CreatedAt, UpdatedAt
            FROM Items
            WHERE CategoryId = @CategoryId
            ORDER BY Priority DESC, DueDate ASC;
            """;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@CategoryId", categoryId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return ReadItem(reader);
        }
    }

    // D7: Added [EnumeratorCancellation]
    public async IAsyncEnumerable<Item> GetAllItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Title, Description, CategoryId, Priority, DueDate, Completed, CreatedAt, UpdatedAt
            FROM Items
            ORDER BY CategoryId, Priority DESC, DueDate ASC;
            """;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return ReadItem(reader);
        }
    }

    public async Task<int> AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO Categories (Name, Color) VALUES (@Name, @Color); SELECT last_insert_rowid();";
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Name", category.Name);
        command.Parameters.AddWithValue("@Color", category.Color);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> AddItemAsync(Item item, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Items (Title, Description, CategoryId, Priority, DueDate, Completed)
            VALUES (@Title, @Description, @CategoryId, @Priority, @DueDate, @Completed);
            SELECT last_insert_rowid();
            """;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Title", item.Title);
        command.Parameters.AddWithValue("@Description", (object?)item.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@CategoryId", item.CategoryId);
        command.Parameters.AddWithValue("@Priority", item.Priority);
        command.Parameters.AddWithValue("@DueDate", (object?)item.DueDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@Completed", item.Completed ? 1 : 0);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> UpdateItemAsync(Item item, CancellationToken cancellationToken = default)
    {
        // D5: Use strftime for ISO-8601
        const string sql = """
            UPDATE Items
            SET Title = @Title, Description = @Description, CategoryId = @CategoryId,
                Priority = @Priority, DueDate = @DueDate, Completed = @Completed,
                UpdatedAt = strftime('%Y-%m-%dT%H:%M:%fZ','now')
            WHERE Id = @Id;
            """;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Id", item.Id);
        command.Parameters.AddWithValue("@Title", item.Title);
        command.Parameters.AddWithValue("@Description", (object?)item.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@CategoryId", item.CategoryId);
        command.Parameters.AddWithValue("@Priority", item.Priority);
        command.Parameters.AddWithValue("@DueDate", (object?)item.DueDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@Completed", item.Completed ? 1 : 0);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Items WHERE Id = @Id;";
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@Id", itemId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<int> GetCompletedCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM Items WHERE Completed = 1;";
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(*) FROM Items WHERE Completed = 0;";
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static Item ReadItem(SqliteDataReader reader)
    {
        return new Item
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            CategoryId = reader.GetInt32(3),
            Priority = reader.GetInt32(4),
            DueDate = reader.IsDBNull(5) ? null : reader.GetString(5),
            Completed = reader.GetInt32(6) == 1,
            CreatedAt = reader.GetString(7),
            UpdatedAt = reader.GetString(8)
        };
    }
}
