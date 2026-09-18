// Closes: B1
using System.Windows;

namespace HayatiDesk;

public partial class App : Application
{
    // B1: Removed async void OnExit. Disposal is handled in MainWindow.OnClosed.
}
