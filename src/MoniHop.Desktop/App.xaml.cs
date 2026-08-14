using System.Windows;
using MoniHop.Desktop.ApplicationProjection;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.Displays;
using MoniHop.Windows.Windows;
using MoniHop.Windows.WindowProjection;

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
            var paths = MoniHopPaths.CreateDefault();
            var profileService = new DisplayProfileService(
                displayCatalog,
                new JsonDisplayProfileStore(paths.DisplayProfilesFile));
            profileService.Refresh();
            var cursorSwitchService = new CursorSwitchService(
                displayCatalog,
                new NativeCursorController());
            var windowSwitchService = new WindowSwitchService(
                displayCatalog,
                new NativeWindowController());
            var applicationWindowController = new NativeApplicationWindowController();
            var installedApplicationCatalog = new NativeInstalledApplicationCatalog();
            var applicationProjectionSettings = new ApplicationProjectionSettingsService(
                new JsonApplicationProjectionStore(paths.ApplicationProjectionFile));
            var applicationProjectionRuntime = new ApplicationProjectionRuntime(
                new NativeWindowEventSource(),
                applicationWindowController,
                displayCatalog,
                applicationProjectionSettings);
            var windowProjectionSettings = new WindowProjectionSettingsService(
                new JsonWindowProjectionStore(paths.WindowProjectionFile));
            var windowProjectionRuntime = new WindowProjectionRuntime(
                new NativeWindowMoveSizeEventSource(),
                new NativePointerState(),
                new WindowProjectionOverlay(),
                applicationWindowController,
                displayCatalog,
                windowProjectionSettings,
                displayProfileService: profileService);

            new MainWindow(
                displays,
                cursorSwitchService,
                windowSwitchService,
                profileService,
                applicationProjectionSettings,
                applicationWindowController,
                installedApplicationCatalog,
                applicationProjectionRuntime,
                windowProjectionSettings,
                windowProjectionRuntime,
                paths).Show();
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
