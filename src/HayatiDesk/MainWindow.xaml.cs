// Closes: R1, D6
using System;
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
        // D6: Move DB to LocalApplicationData
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDir = Path.Combine(appData, "HayatiDesk");
        Directory.CreateDirectory(dbDir);
        var databasePath = Path.Combine(dbDir, "hayatidesk.db");

        _databaseContext = new DatabaseContext(databasePath);
        var repository = new ItemRepository(_databaseContext);
        _viewModel = new MainViewModel(repository);
        
        InitializeComponent();
        DataContext = _viewModel;
        
        // R1: Await initialization in Loaded event with try/catch
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        try
        {
            await _databaseContext.InitializeAsync();
            await _viewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            _viewModel.ErrorMessage = ex.Message;
        }
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        await _viewModel.DisposeAsync();
        await _databaseContext.DisposeAsync();
    }
}
