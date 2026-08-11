using System.Windows;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.Displays;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var displayCatalog = new NativeDisplayCatalog();
            var displays = displayCatalog.ReadAll();
            var cursorSwitchService = new CursorSwitchService(
                displayCatalog,
                new NativeCursorController());
            var windowSwitchService = new WindowSwitchService(
                displayCatalog,
                new NativeWindowController());

            new MainWindow(displays, cursorSwitchService, windowSwitchService).Show();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"无法读取显示器信息。\n\n{exception.Message}",
                "MoniHop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
