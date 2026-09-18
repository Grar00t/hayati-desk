// Closes: T1
using System;
using System.IO;
using System.Threading.Tasks;
using HayatiDesk.Data;
using HayatiDesk.Services;
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
    public async Task AddCategoryAsync_InsertsCategory_ReturnsId()
    {
        var repository = new ItemRepository(_databaseContext);
        var category = new Category { Name = "Test Category", Color = "#FF0000" };
        
        var id = await repository.AddCategoryAsync(category);
        
        Assert.True(id > 0);
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
}
