// Closes: R2, R3, U2, U3
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HayatiDesk.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace HayatiDesk.ViewModels;

public partial class MainViewModel : ObservableObject, IAsyncDisposable
{
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

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<ItemViewModel> Items { get; } = [];

    // U2: Removed DatabaseContext dependency. ViewModel only depends on IItemRepository.
    public MainViewModel(IItemRepository itemRepository)
    {
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

        // R2: Seed default categories if empty
        if (Categories.Count == 0)
        {
            await _itemRepository.AddCategoryAsync(new Category { Name = "General", Color = "#000000" });
            await _itemRepository.AddCategoryAsync(new Category { Name = "Work", Color = "#FF0000" });
            await _itemRepository.AddCategoryAsync(new Category { Name = "Personal", Color = "#00FF00" });
            
            Categories.Clear();
            await foreach (var category in _itemRepository.GetAllCategoriesAsync())
            {
                Categories.Add(category);
            }
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
            // U3: Marshal to UI thread
            Application.Current?.Dispatcher.Invoke(() => Items.Add(new ItemViewModel(item)), DispatcherPriority.Normal);
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

        Application.Current?.Dispatcher.Invoke(() => Items.Add(new ItemViewModel(newItem)), DispatcherPriority.Normal);
        NewItemTitle = string.Empty;
        NewItemDescription = string.Empty;
        await UpdateStatsAsync();
    }

    // R3: Exactly one owner of the flip. Command flips and persists.
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
        Application.Current?.Dispatcher.Invoke(() => Items.Remove(item), DispatcherPriority.Normal);
        await UpdateStatsAsync();
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        return ValueTask.CompletedTask;
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
