using System.Windows;
using MoniHop.Windows.Displays;

namespace MoniHop.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var displays = new NativeDisplayCatalog().ReadAll();
            new MainWindow(displays).Show();
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
