using System.Net.Http;
using System.Windows;
using System.Windows.Interop;
using MoniHop.Desktop.ApplicationProjection;
using MoniHop.Desktop.Diagnostics;
using MoniHop.Desktop.HotKeys;
using MoniHop.Desktop.Lifecycle;
using MoniHop.Desktop.Localization;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.Theming;
using MoniHop.Desktop.Updates;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.Displays;
using MoniHop.Windows.Security;
using MoniHop.Windows.Startup;
using MoniHop.Windows.Windows;
using MoniHop.Windows.WindowProjection;

namespace MoniHop.Desktop;

public partial class App : Application
{
    private SingleInstanceCoordinator? _singleInstance;
    private HttpClient? _updateCheckHttpClient;
    private HttpClient? _updateDownloadHttpClient;
    private LocalDiagnosticService? _diagnostics;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        GeneralSettingsService? generalSettings = null;
        try
        {
            var paths = MoniHopPaths.CreateDefault();
            _diagnostics = new LocalDiagnosticService(
                paths.DiagnosticLogFile,
                () => generalSettings?.Current.DetailedDiagnosticsEnabled == true);
            var displayCatalog = new NativeDisplayCatalog();
            var displays = displayCatalog.ReadAll();
            generalSettings = new GeneralSettingsService(
                new JsonGeneralSettingsStore(paths.GeneralSettingsFile));
            var localization = new LocalizationService();
            localization.Apply(generalSettings.Current.Language);
            var theme = new ThemeService();
            theme.Apply(generalSettings.Current.Theme);
            var elevation = new NativeProcessElevationService();
            var executablePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Executable path is unavailable.");
            if (StartupLaunchPolicy.Decide(
                    generalSettings.Current.AlwaysRunAsAdministrator,
                    elevation.IsElevated) == StartupLaunchAction.RestartElevatedBeforeSingleInstance)
            {
                var restart = elevation.RestartElevated(executablePath, e.Args);
                if (restart == ElevationRestartResult.Started)
                {
                    Shutdown();
                    return;
                }
            }

            var isElevationRestart = e.Args.Contains(
                "--elevation-restart",
                StringComparer.OrdinalIgnoreCase);
            _singleInstance = new SingleInstanceCoordinator(
                "MoniHop.Desktop.SingleInstance.v1",
                isElevationRestart ? TimeSpan.FromSeconds(5) : null);
            if (!_singleInstance.IsPrimary)
            {
                _singleInstance.SignalPrimaryAsync().GetAwaiter().GetResult();
                Shutdown();
                return;
            }

            var startup = new NativeStartupRegistrationService(
                executablePath);
            var profileService = new DisplayProfileService(
                displayCatalog,
                new JsonDisplayProfileStore(paths.DisplayProfilesFile));
            profileService.Refresh();
            var cursorSwitchService = new CursorSwitchService(
                displayCatalog,
                new NativeCursorController(),
                landingMode: () => generalSettings.Current.CursorLanding);
            var nativeWindowController = new NativeWindowController();
            var windowSwitchService = new WindowSwitchService(
                displayCatalog,
                nativeWindowController);
            var applicationWindowController = new NativeApplicationWindowController(nativeWindowController);
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
            var hotKeySettings = new HotKeySettingsService(
                new JsonHotKeyStore(paths.HotKeysFile));
            _updateCheckHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _updateDownloadHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            var updateCheckService = new GitHubUpdateCheckService(_updateCheckHttpClient, ProductInfo.Version);
            var updateInstallerService = new UpdateInstallerService(
                _updateDownloadHttpClient,
                new NativeUpdateInstallerLauncher());
            var projectionShortcutService = new WindowProjectionShortcutService(
                displayCatalog,
                nativeWindowController,
                applicationWindowController,
                windowProjectionSettings);
            var offscreenWindowRecallService = new OffscreenWindowRecallService(
                displayCatalog,
                applicationWindowController,
                new NativeVirtualDesktopWindowFilter());
            var hotKeyExecutor = new HotKeyActionExecutor(
                cursorSwitchService,
                windowSwitchService,
                projectionShortcutService,
                offscreenWindowRecallService,
                applicationProjectionSettings);
            var windowProjectionRuntime = new WindowProjectionRuntime(
                new NativeWindowMoveSizeEventSource(),
                new NativePointerState(),
                new WindowProjectionOverlay(),
                applicationWindowController,
                displayCatalog,
                windowProjectionSettings,
                displayProfileService: profileService);

            var mainWindow = new MainWindow(
                displays,
                hotKeyExecutor,
                profileService,
                applicationProjectionSettings,
                applicationWindowController,
                installedApplicationCatalog,
                applicationProjectionRuntime,
                windowProjectionSettings,
                windowProjectionRuntime,
                hotKeySettings,
                generalSettings,
                startup,
                elevation,
                theme,
                localization,
                offscreenWindowRecallService,
                updateCheckService,
                updateInstallerService,
                _diagnostics,
                paths);
            MainWindow = mainWindow;
            _singleInstance.ShowRequested += (_, _) =>
                Dispatcher.Invoke(mainWindow.ShowAndActivate);
            _ = new WindowInteropHelper(mainWindow).EnsureHandle();
            if (!e.Args.Contains("--background", StringComparer.OrdinalIgnoreCase))
            {
                mainWindow.Show();
            }

            _diagnostics.Write("startup.ready", "MoniHop started successfully.");
            if (JsonStoreRecovery.RecoveryCount > 0)
            {
                _diagnostics.Write(
                    "configuration.recovered",
                    $"RecoveredFileCount={JsonStoreRecovery.RecoveryCount}",
                    always: true);
            }
        }
        catch (Exception exception)
        {
            _diagnostics?.Write("startup.failed", exception.GetType().Name, always: true);
            MessageBox.Show(
                $"MoniHop 启动失败。\n\n{exception.Message}",
                "MoniHop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _diagnostics?.Write("shutdown", "MoniHop is exiting.");
        _updateCheckHttpClient?.Dispose();
        _updateDownloadHttpClient?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
