using System.Windows;

namespace HayatiDesk;

public partial class App : Application
{
    protected override async void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        
        if (MainWindow is MainWindow mainWindow)
        {
            await mainWindow.Dispatcher.InvokeAsync(async () =>
            {
                await mainWindow.DisposeAsync();
            });
        }
    }
}

