// Closes: T1
using System;
using System.IO;
using System.Threading.Tasks;
using HayatiDesk.Data;
using HayatiDesk.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HayatiDesk.Tests;

// T1: Renamed from UnitTest1.cs / DatabaseContextTests
public class ItemRepositoryTests : IAsyncLifetime
{
    private readonly string _testDbPath;
    private readonly DatabaseContext _databaseContext;

    public ItemRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"hayatidesk_test_{Guid.NewGuid()}.db");
        _databaseContext = new DatabaseContext(_testDbPath);
    }

    public async Task InitializeAsync()
    {
        await _databaseContext.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _databaseContext.DisposeAsync();
        
        // T1: Clean up -wal and -shm
        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
            if (File.Exists(_testDbPath + "-wal")) File.Delete(_testDbPath + "-wal");
            if (File.Exists(_testDbPath + "-shm")) File.Delete(_testDbPath + "-shm");
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task InitializeAsync_RejectsDatabaseFromNewerSchemaVersion()
    {
        var newerDbPath = Path.Combine(Path.GetTempPath(), $"hayatidesk_newer_{Guid.NewGuid()}.db");

        try
        {
            await using (var connection = new SqliteConnection($"Data Source={newerDbPath};Pooling=False"))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA user_version = 2;";
                await command.ExecuteNonQueryAsync();
            }

            await using var context = new DatabaseContext(newerDbPath);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.InitializeAsync());

            Assert.Contains("newer than supported", error.Message);

            await using var verifyConnection = new SqliteConnection($"Data Source={newerDbPath};Pooling=False");
            await verifyConnection.OpenAsync();
            await using var verifyCommand = verifyConnection.CreateCommand();
            verifyCommand.CommandText = "PRAGMA user_version;";
            Assert.Equal(2L, (long)(await verifyCommand.ExecuteScalarAsync())!);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(newerDbPath)) File.Delete(newerDbPath);
            if (File.Exists(newerDbPath + "-wal")) File.Delete(newerDbPath + "-wal");
            if (File.Exists(newerDbPath + "-shm")) File.Delete(newerDbPath + "-shm");
        }
    }

    [Fact]
    public async Task AddCategoryAsync_InsertsCategory_ReturnsId()
    {
        var repository = new ItemRepository(_databaseContext);
        var category = new Category { Name = "Test Category", Color = "#FF0000" };
        
        var id = await repository.AddCategoryAsync(category);
        
        Assert.True(id > 0);
    }

    [Fact]
    public async Task SeedDefaultCategoriesIfEmptyAsync_SeedsExactlyOnce()
    {
        var repository = new ItemRepository(_databaseContext);

        var firstInsertCount = await repository.SeedDefaultCategoriesIfEmptyAsync();
        var secondInsertCount = await repository.SeedDefaultCategoriesIfEmptyAsync();

        var rows = new System.Collections.Generic.List<Category>();
        await foreach (var row in repository.GetAllCategoriesAsync()) rows.Add(row);

        Assert.Equal(3, firstInsertCount);
        Assert.Equal(0, secondInsertCount);
        Assert.Equal(3, rows.Count);
        Assert.Contains(rows, category => category.Name == "General");
        Assert.Contains(rows, category => category.Name == "Work");
        Assert.Contains(rows, category => category.Name == "Personal");
    }

    [Fact]
    public async Task AddItemAsync_InsertsItem_ReturnsId()
    {
        var repository = new ItemRepository(_databaseContext);
        var category = new Category { Name = "Test", Color = "#000000" };
        var categoryId = await repository.AddCategoryAsync(category);
        
        var item = new Item 
        { 
            Title = "Test Item", 
            CategoryId = categoryId,
            Priority = 1,
            Completed = false
        };
        
        var id = await repository.AddItemAsync(item);
        
        Assert.True(id > 0);
    }

    [Fact]
    public async Task ItemFields_RoundTripUpdateAndDelete()
    {
        var repository = new ItemRepository(_databaseContext);
        var categoryId = await repository.AddCategoryAsync(new Category { Name = "Round trip" });
        var item = new Item
        {
            Title = "Task", Description = "Details", CategoryId = categoryId,
            Priority = 2, DueDate = "2026-10-01", Completed = true
        };
        item.Id = await repository.AddItemAsync(item);
        var rows = new System.Collections.Generic.List<Item>();
        await foreach (var row in repository.GetItemsByCategoryAsync(categoryId)) rows.Add(row);
        var stored = Assert.Single(rows);
        Assert.Equal(item.Title, stored.Title);
        Assert.Equal(item.Description, stored.Description);
        Assert.Equal(item.Priority, stored.Priority);
        Assert.Equal(item.DueDate, stored.DueDate);
        Assert.True(stored.Completed);
        Assert.False(string.IsNullOrWhiteSpace(stored.CreatedAt));
        Assert.False(string.IsNullOrWhiteSpace(stored.UpdatedAt));
        Assert.Equivalent(stored, new HayatiDesk.ViewModels.ItemViewModel(stored).ToItem());
        Assert.Equal(1, await repository.GetCompletedCountAsync());
        stored.Completed = false;
        stored.Description = null;
        stored.DueDate = null;
        Assert.True(await repository.UpdateItemAsync(stored));
        rows.Clear();
        await foreach (var row in repository.GetAllItemsAsync()) rows.Add(row);
        stored = Assert.Single(rows);
        Assert.Null(stored.Description);
        Assert.Null(stored.DueDate);
        Assert.False(stored.Completed);
        Assert.Equal(1, await repository.GetPendingCountAsync());
        Assert.True(await repository.DeleteItemAsync(stored.Id));
        Assert.Equal(0, await repository.GetPendingCountAsync());
    }

    [Fact]
    public async Task CategoryFields_RoundTripWithSchemaDefaults()
    {
        var repository = new ItemRepository(_databaseContext);
        var id = await repository.AddCategoryAsync(new Category { Name = "Default color" });
        var rows = new System.Collections.Generic.List<Category>();
        await foreach (var row in repository.GetAllCategoriesAsync()) rows.Add(row);
        var category = Assert.Single(rows);
        Assert.Equal(id, category.Id);
        Assert.Equal("Default color", category.Name);
        Assert.Equal("#000000", category.Color);
        Assert.False(string.IsNullOrWhiteSpace(category.CreatedAt));
    }

    [Fact]
    public void IncompleteItem_HasNoStrikethroughDecoration()
    {
        var converter = new StrikethroughConverter();
        Assert.Null(converter.Convert(false, typeof(object), null!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.NotNull(converter.Convert(true, typeof(object), null!, System.Globalization.CultureInfo.InvariantCulture));
    }
}
