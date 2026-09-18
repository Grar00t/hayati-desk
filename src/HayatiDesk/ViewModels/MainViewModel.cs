using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HayatiDesk.Data;
using HayatiDesk.Services;
using System.Collections.ObjectModel;

namespace HayatiDesk.ViewModels;

public partial class MainViewModel : ObservableObject, IAsyncDisposable
{
    private readonly DatabaseContext _databaseContext;
    private readonly IItemRepository _itemRepository;
    private bool _disposed;

    [ObservableProperty]
    private string _newItemTitle = string.Empty;

    [ObservableProperty]
    private string _newItemDescription = string.Empty;

    [ObservableProperty]
    private int _selectedCategoryId;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _pendingCount;

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<ItemViewModel> Items { get; } = [];

    public MainViewModel(DatabaseContext databaseContext, IItemRepository itemRepository)
    {
        _databaseContext = databaseContext;
        _itemRepository = itemRepository;
    }

    public async Task InitializeAsync()
    {
        await LoadCategoriesAsync();
        await UpdateStatsAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        Categories.Clear();
        await foreach (var category in _itemRepository.GetAllCategoriesAsync())
        {
            Categories.Add(category);
        }

        if (Categories.Count > 0 && SelectedCategory == null)
        {
            SelectedCategory = Categories[0];
            SelectedCategoryId = SelectedCategory.Id;
            await LoadItemsAsync(SelectedCategory.Id);
        }
    }

    private async Task LoadItemsAsync(int categoryId)
    {
        Items.Clear();
        await foreach (var item in _itemRepository.GetItemsByCategoryAsync(categoryId))
        {
            Items.Add(new ItemViewModel(item));
        }
    }

    private async Task UpdateStatsAsync()
    {
        CompletedCount = await _itemRepository.GetCompletedCountAsync();
        PendingCount = await _itemRepository.GetPendingCountAsync();
    }

    [RelayCommand]
    private async Task SelectCategoryAsync(Category? category)
    {
        if (category == null) return;

        SelectedCategory = category;
        SelectedCategoryId = category.Id;
        await LoadItemsAsync(category.Id);
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemTitle) || SelectedCategory == null)
            return;

        var newItem = new Item
        {
            Title = NewItemTitle,
            Description = NewItemDescription,
            CategoryId = SelectedCategory.Id,
            Priority = 0,
            Completed = false
        };

        var id = await _itemRepository.AddItemAsync(newItem);
        newItem.Id = id;

        Items.Add(new ItemViewModel(newItem));
        NewItemTitle = string.Empty;
        NewItemDescription = string.Empty;
        await UpdateStatsAsync();
    }

    [RelayCommand]
    private async Task ToggleItemCompletionAsync(ItemViewModel item)
    {
        if (item == null) return;

        item.Completed = !item.Completed;
        await _itemRepository.UpdateItemAsync(item.ToItem());
        await UpdateStatsAsync();
    }

    [RelayCommand]
    private async Task DeleteItemAsync(ItemViewModel item)
    {
        if (item == null) return;

        await _itemRepository.DeleteItemAsync(item.Id);
        Items.Remove(item);
        await UpdateStatsAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _databaseContext.DisposeAsync();
            _disposed = true;
        }
    }
}

public partial class ItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private int _categoryId;

    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private string? _dueDate;

    [ObservableProperty]
    private bool _completed;

    [ObservableProperty]
    private string _createdAt = string.Empty;

    [ObservableProperty]
    private string _updatedAt = string.Empty;

    public ItemViewModel() { }

    public ItemViewModel(Item item)
    {
        Id = item.Id;
        Title = item.Title;
        Description = item.Description;
        CategoryId = item.CategoryId;
        Priority = item.Priority;
        DueDate = item.DueDate;
        Completed = item.Completed;
        CreatedAt = item.CreatedAt;
        UpdatedAt = item.UpdatedAt;
    }

    public Item ToItem() => new()
    {
        Id = Id,
        Title = Title,
        Description = Description,
        CategoryId = CategoryId,
        Priority = Priority,
        DueDate = DueDate,
        Completed = Completed,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };
}
