using HayatiDesk.Data;
using HayatiDesk.Services;

namespace HayatiDesk.Tests;

public class DatabaseContextTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly DatabaseContext _databaseContext;

    public DatabaseContextTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"hayatidesk_test_{Guid.NewGuid()}.db");
        _databaseContext = new DatabaseContext(_testDbPath);
        _databaseContext.InitializeAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task InitializeAsync_CreatesTables_Successfully()
    {
        await using var command = _databaseContext.Connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Categories';";
        var result = await command.ExecuteScalarAsync();
        
        Assert.NotNull(result);
        Assert.Equal("Categories", result.ToString());
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

    [Fact]
    public async Task GetAllCategoriesAsync_ReturnsAllCategories()
    {
        var repository = new ItemRepository(_databaseContext);
        await repository.AddCategoryAsync(new Category { Name = "Cat1", Color = "#111111" });
        await repository.AddCategoryAsync(new Category { Name = "Cat2", Color = "#222222" });
        
        var categories = new List<Category>();
        await foreach (var cat in repository.GetAllCategoriesAsync())
        {
            categories.Add(cat);
        }
        
        Assert.Equal(2, categories.Count);
        Assert.Contains(categories, c => c.Name == "Cat1");
        Assert.Contains(categories, c => c.Name == "Cat2");
    }

    [Fact]
    public async Task UpdateItemAsync_UpdatesItem_ReturnsTrue()
    {
        var repository = new ItemRepository(_databaseContext);
        var category = new Category { Name = "Test", Color = "#000000" };
        var categoryId = await repository.AddCategoryAsync(category);
        
        var item = new Item 
        { 
            Title = "Original", 
            CategoryId = categoryId,
            Completed = false
        };
        var id = await repository.AddItemAsync(item);
        item.Id = id;
        item.Title = "Updated";
        
        var result = await repository.UpdateItemAsync(item);
        
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteItemAsync_DeletesItem_ReturnsTrue()
    {
        var repository = new ItemRepository(_databaseContext);
        var category = new Category { Name = "Test", Color = "#000000" };
        var categoryId = await repository.AddCategoryAsync(category);
        
        var item = new Item 
        { 
            Title = "To Delete", 
            CategoryId = categoryId,
            Completed = false
        };
        var id = await repository.AddItemAsync(item);
        
        var result = await repository.DeleteItemAsync(id);
        
        Assert.True(result);
    }

    [Fact]
    public async Task GetCompletedCountAsync_ReturnsCorrectCount()
    {
        var repository = new ItemRepository(_databaseContext);
        var category = new Category { Name = "Test", Color = "#000000" };
        var categoryId = await repository.AddCategoryAsync(category);
        
        await repository.AddItemAsync(new Item { Title = "Item1", CategoryId = categoryId, Completed = true });
        await repository.AddItemAsync(new Item { Title = "Item2", CategoryId = categoryId, Completed = true });
        await repository.AddItemAsync(new Item { Title = "Item3", CategoryId = categoryId, Completed = false });
        
        var completedCount = await repository.GetCompletedCountAsync();
        
        Assert.Equal(2, completedCount);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
