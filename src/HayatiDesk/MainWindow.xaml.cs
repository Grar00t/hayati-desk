using System.IO;
using System.Windows;
using HayatiDesk.Data;
using HayatiDesk.Services;
using HayatiDesk.ViewModels;

namespace HayatiDesk;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DatabaseContext _databaseContext;

    public MainWindow()
    {
        var databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hayatidesk.db");
        _databaseContext = new DatabaseContext(databasePath);
        _viewModel = new MainViewModel(_databaseContext, new ItemRepository(_databaseContext));
        
        InitializeComponent();
        DataContext = _viewModel;
        
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _viewModel.InitializeAsync();
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        await _viewModel.DisposeAsync();
    }
}