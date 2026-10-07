using System.Collections.Generic;
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
    public async Task Toggle_rolls_back_when_repository_throws()
    {
        var repository = new StubRepository { UpdateException = new InvalidOperationException("write failed") };
        var viewModel = new MainViewModel(repository);
        var item = new ItemViewModel(new Item { Id = 7, Title = "x", CategoryId = 1, Completed = false });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => viewModel.ToggleItemCompletionCommand.ExecuteAsync(item));

        Assert.False(item.Completed);
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

    private sealed class StubRepository : IItemRepository
    {
        public bool UpdateResult { get; init; } = true;
        public bool DeleteResult { get; init; } = true;
        public Exception? UpdateException { get; init; }

        public async IAsyncEnumerable<Category> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<Item> GetItemsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<Item> GetAllItemsAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<int> AddCategoryAsync(Category category, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<int> AddItemAsync(Item item, CancellationToken cancellationToken = default) =>
            Task.FromResult(1);

        public Task<bool> UpdateItemAsync(Item item, CancellationToken cancellationToken = default)
        {
            if (UpdateException is not null)
                return Task.FromException<bool>(UpdateException);
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteItemAsync(int itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DeleteResult);

        public Task<int> GetCompletedCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
