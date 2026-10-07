using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HayatiDesk.Services;
using HayatiDesk.ViewModels;

namespace HayatiDesk.Tests;

public class MainViewModelTests
{
    [Fact]
    public async Task Toggle_rolls_back_when_repository_reports_no_update()
    {
        var repository = new StubRepository { UpdateResult = false };
        var viewModel = new MainViewModel(repository);
        var item = new ItemViewModel(new Item { Id = 7, Title = "x", CategoryId = 1, Completed = false });

        await viewModel.ToggleItemCompletionCommand.ExecuteAsync(item);

        Assert.False(item.Completed);
        Assert.Equal("Item update was not persisted.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Toggle_rolls_back_and_surfaces_error_when_repository_throws()
    {
        var repository = new StubRepository { UpdateException = new InvalidOperationException("write failed") };
        var viewModel = new MainViewModel(repository);
        var item = new ItemViewModel(new Item { Id = 7, Title = "x", CategoryId = 1, Completed = false });

        await viewModel.ToggleItemCompletionCommand.ExecuteAsync(item);

        Assert.False(item.Completed);
        Assert.Equal("Could not update the item.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Add_preserves_input_and_surfaces_error_when_repository_throws()
    {
        var repository = new StubRepository { AddException = new InvalidOperationException("write failed") };
        var viewModel = new MainViewModel(repository)
        {
            SelectedCategory = new Category { Id = 1, Name = "General" },
            NewItemTitle = "Keep me",
            NewItemDescription = "Still here"
        };

        await viewModel.AddItemCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Items);
        Assert.Equal("Keep me", viewModel.NewItemTitle);
        Assert.Equal("Still here", viewModel.NewItemDescription);
        Assert.Equal("Could not add the item.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Delete_keeps_item_when_repository_reports_no_delete()
    {
        var repository = new StubRepository { DeleteResult = false };
        var viewModel = new MainViewModel(repository);
        var item = new ItemViewModel(new Item { Id = 7, Title = "x", CategoryId = 1 });
        viewModel.Items.Add(item);

        await viewModel.DeleteItemCommand.ExecuteAsync(item);

        Assert.Contains(item, viewModel.Items);
        Assert.Equal("Item was not deleted.", viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Delete_removes_item_after_confirmed_persistence_delete()
    {
        var repository = new StubRepository { DeleteResult = true };
        var viewModel = new MainViewModel(repository);
        var item = new ItemViewModel(new Item { Id = 7, Title = "x", CategoryId = 1 });
        viewModel.Items.Add(item);

        await viewModel.DeleteItemCommand.ExecuteAsync(item);

        Assert.DoesNotContain(item, viewModel.Items);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Delete_keeps_item_and_surfaces_error_when_repository_throws()
    {
        var repository = new StubRepository { DeleteException = new InvalidOperationException("write failed") };
        var viewModel = new MainViewModel(repository);
        var item = new ItemViewModel(new Item { Id = 7, Title = "x", CategoryId = 1 });
        viewModel.Items.Add(item);

        await viewModel.DeleteItemCommand.ExecuteAsync(item);

        Assert.Contains(item, viewModel.Items);
        Assert.Equal("Could not delete the item.", viewModel.ErrorMessage);
    }

    private sealed class StubRepository : IItemRepository
    {
        public bool UpdateResult { get; init; } = true;
        public bool DeleteResult { get; init; } = true;
        public Exception? AddException { get; init; }
        public Exception? UpdateException { get; init; }
        public Exception? DeleteException { get; init; }

        public async IAsyncEnumerable<Category> GetAllCategoriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<Item> GetItemsByCategoryAsync(int categoryId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<Item> GetAllItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<int> SeedDefaultCategoriesIfEmptyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> AddCategoryAsync(Category category, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<int> AddItemAsync(Item item, CancellationToken cancellationToken = default)
        {
            if (AddException is not null)
                return Task.FromException<int>(AddException);
            return Task.FromResult(1);
        }

        public Task<bool> UpdateItemAsync(Item item, CancellationToken cancellationToken = default)
        {
            if (UpdateException is not null)
                return Task.FromException<bool>(UpdateException);
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteItemAsync(int itemId, CancellationToken cancellationToken = default)
        {
            if (DeleteException is not null)
                return Task.FromException<bool>(DeleteException);
            return Task.FromResult(DeleteResult);
        }

        public Task<int> GetCompletedCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
