using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using MoniHop.Core.Displays;
using MoniHop.Core.Windows;
using MoniHop.Desktop.ApplicationProjection;
using MoniHop.Desktop.Diagnostics;
using MoniHop.Desktop.HotKeys;
using MoniHop.Desktop.Localization;
using MoniHop.Desktop.Lifecycle;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.Views;
using MoniHop.Desktop.Theming;
using MoniHop.Desktop.Updates;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.HotKeys;
using MoniHop.Windows.Security;
using MoniHop.Windows.Startup;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop;

public partial class MainWindow : Window
{
    private const int FirstDynamicHotKeyId = 0x5200;

    private readonly HotKeyActionExecutor _hotKeyExecutor;
    private readonly List<GlobalHotKeyRegistration> _hotKeyRegistrations = [];
    private readonly Dictionary<int, HotKeyStatusViewModel> _registeredHotKeys = [];
    private readonly IReadOnlyList<NavigationItem> _navigationItems;
    private readonly DisplayProfileService _displayProfileService;
    private readonly DisplaysPage _displaysPage;
    private readonly ProjectionPage _projectionPage;
    private readonly ApplicationProjectionPage _applicationProjectionPage;
    private readonly ApplicationProjectionSettingsService _applicationProjectionSettings;
    private readonly ApplicationProjectionRuntime _applicationProjectionRuntime;
    private readonly WindowProjectionRuntime _windowProjectionRuntime;
    private readonly WindowProjectionSettingsService _windowProjectionSettings;
    private readonly HotKeySettingsService _hotKeySettings;
    private readonly GeneralSettingsService _generalSettings;
    private readonly OffscreenWindowRecallService _offscreenWindowRecallService;
    private readonly IProcessElevationService _processElevation;
    private readonly LocalizationService _localization;
    private readonly ThemeService _themeService;
    private readonly BehaviorPage _behaviorPage;
    private readonly AboutPage _aboutPage;
    private readonly LocalDiagnosticService _diagnostics;
    private readonly TrayIconService _trayIcon;
    private readonly DispatcherTimer _displayRefreshTimer;
    private HwndSource? _windowSource;
    private nint _windowHandle;
    private QuickProjectionWindow? _quickProjectionWindow;
    private bool _isExiting;

    public MainWindow(
        IReadOnlyList<DisplaySnapshot> displays,
        HotKeyActionExecutor hotKeyExecutor,
        DisplayProfileService displayProfileService,
        ApplicationProjectionSettingsService applicationProjectionSettings,
        IApplicationWindowController applicationWindowController,
        IInstalledApplicationCatalog installedApplicationCatalog,
        ApplicationProjectionRuntime applicationProjectionRuntime,
        WindowProjectionSettingsService windowProjectionSettings,
        WindowProjectionRuntime windowProjectionRuntime,
        HotKeySettingsService hotKeySettings,
        GeneralSettingsService generalSettings,
        IStartupRegistrationService startupRegistration,
        IProcessElevationService processElevation,
        ThemeService themeService,
        LocalizationService localizationService,
        OffscreenWindowRecallService offscreenWindowRecallService,
        GitHubUpdateCheckService updateCheckService,
        LocalDiagnosticService diagnostics,
        MoniHopPaths paths)
    {
        ArgumentNullException.ThrowIfNull(displays);
        _hotKeyExecutor = hotKeyExecutor ?? throw new ArgumentNullException(nameof(hotKeyExecutor));
        _displayProfileService = displayProfileService ?? throw new ArgumentNullException(nameof(displayProfileService));
        _applicationProjectionSettings = applicationProjectionSettings ?? throw new ArgumentNullException(nameof(applicationProjectionSettings));
        ArgumentNullException.ThrowIfNull(applicationWindowController);
        ArgumentNullException.ThrowIfNull(installedApplicationCatalog);
        _applicationProjectionRuntime = applicationProjectionRuntime ?? throw new ArgumentNullException(nameof(applicationProjectionRuntime));
        _windowProjectionSettings = windowProjectionSettings ?? throw new ArgumentNullException(nameof(windowProjectionSettings));
        _windowProjectionRuntime = windowProjectionRuntime ?? throw new ArgumentNullException(nameof(windowProjectionRuntime));
        _hotKeySettings = hotKeySettings ?? throw new ArgumentNullException(nameof(hotKeySettings));
        _generalSettings = generalSettings ?? throw new ArgumentNullException(nameof(generalSettings));
        ArgumentNullException.ThrowIfNull(startupRegistration);
        _processElevation = processElevation ?? throw new ArgumentNullException(nameof(processElevation));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _localization = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _offscreenWindowRecallService = offscreenWindowRecallService ?? throw new ArgumentNullException(nameof(offscreenWindowRecallService));
        ArgumentNullException.ThrowIfNull(updateCheckService);
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        ArgumentNullException.ThrowIfNull(paths);

        HotKeys = [];
        RebuildHotKeys();

        InitializeComponent();

        _displayRefreshTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(250),
            DispatcherPriority.Background,
            DisplayRefreshTimer_OnTick,
            Dispatcher)
        {
            IsEnabled = false,
        };

        _displaysPage = new DisplaysPage(_displayProfileService, applicationProjectionSettings);
        _applicationProjectionPage = new ApplicationProjectionPage(
            applicationProjectionSettings,
            _displayProfileService,
            applicationWindowController,
            installedApplicationCatalog,
            _generalSettings);
        _projectionPage = new ProjectionPage(
            _windowProjectionSettings,
            _displayProfileService,
            _generalSettings);
        _behaviorPage = new BehaviorPage(
            _generalSettings,
            startupRegistration,
            _processElevation,
            _themeService,
            _localization);
        _aboutPage = new AboutPage(
            paths,
            _generalSettings,
            updateCheckService,
            _diagnostics,
            _displayProfileService,
            _processElevation,
            _localization);
        _behaviorPage.RestartElevatedRequested += BehaviorPage_OnRestartElevatedRequested;
        _trayIcon = new TrayIconService(_localization);
        _trayIcon.OpenRequested += TrayIcon_OnOpenRequested;
        _trayIcon.ToggleApplicationProjectionRequested += TrayIcon_OnToggleApplicationProjectionRequested;
        _trayIcon.RecallRequested += TrayIcon_OnRecallRequested;
        _trayIcon.ExitRequested += TrayIcon_OnExitRequested;
        _trayIcon.SetApplicationProjectionEnabled(_applicationProjectionSettings.Current.IsEnabled);
        _displayProfileService.Changed += DisplayProfileService_OnChanged;
        _displayProfileService.TopologyChanged += DisplayProfileService_OnTopologyChanged;
        _hotKeySettings.Changed += HotKeySettings_OnChanged;
        _applicationProjectionSettings.Changed += ApplicationProjectionSettings_OnChanged;
        _hotKeyExecutor.ProjectionPanelRequested += HotKeyExecutor_OnProjectionPanelRequested;
        _hotKeyExecutor.SettingsRequested += HotKeyExecutor_OnSettingsRequested;
        _applicationProjectionRuntime.ProjectionCompleted += ApplicationProjectionRuntime_OnProjectionCompleted;
        _windowProjectionRuntime.ProjectionCompleted += WindowProjectionRuntime_OnProjectionCompleted;
        _localization.Changed += Localization_OnChanged;
        _navigationItems =
        [
            new("显示器", "\uE7F4", _displaysPage),
            new("窗口投放", "\uE8A7", _projectionPage),
            new("应用投放", "\uE8FD", _applicationProjectionPage),
            new("快捷键", "\uE765", new HotKeysPage(HotKeys, _hotKeySettings, _generalSettings)),
            new("通用设置", "\uE713", _behaviorPage),
            new("关于与诊断", "\uE946", _aboutPage),
        ];

        NavigationList.ItemsSource = _navigationItems;
        NavigationList.SelectedIndex = 0;
        _ = _aboutPage.CheckForUpdatesOnStartupAsync();
    }

    public ObservableCollection<HotKeyStatusViewModel> HotKeys { get; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(_windowHandle);
        _windowSource.AddHook(WindowHook);

        RegisterConfiguredHotKeys();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isExiting)
        {
            base.OnClosing(e);
            return;
        }

        var resolution = CloseBehaviorController.Resolve(
            _generalSettings.Current.CloseBehavior,
            ShowClosePrompt);
        if (resolution.RememberedBehavior is { } remembered)
        {
            if (!_generalSettings.Update(_generalSettings.Current with { CloseBehavior = remembered }))
            {
                _diagnostics.Write("settings.save.failed", "CloseBehavior", always: true);
            }

            _behaviorPage.Refresh();
        }

        switch (resolution.Action)
        {
            case CloseAction.Hide:
                e.Cancel = true;
                Hide();
                return;
            case CloseAction.Cancel:
                e.Cancel = true;
                return;
            case CloseAction.Exit:
                _isExiting = true;
                break;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _displayRefreshTimer.Stop();
        _displayProfileService.Changed -= DisplayProfileService_OnChanged;
        _displayProfileService.TopologyChanged -= DisplayProfileService_OnTopologyChanged;
        _hotKeySettings.Changed -= HotKeySettings_OnChanged;
        _applicationProjectionSettings.Changed -= ApplicationProjectionSettings_OnChanged;
        _hotKeyExecutor.ProjectionPanelRequested -= HotKeyExecutor_OnProjectionPanelRequested;
        _hotKeyExecutor.SettingsRequested -= HotKeyExecutor_OnSettingsRequested;
        _applicationProjectionRuntime.ProjectionCompleted -= ApplicationProjectionRuntime_OnProjectionCompleted;
        _windowProjectionRuntime.ProjectionCompleted -= WindowProjectionRuntime_OnProjectionCompleted;
        _localization.Changed -= Localization_OnChanged;
        _behaviorPage.RestartElevatedRequested -= BehaviorPage_OnRestartElevatedRequested;
        _applicationProjectionRuntime.Dispose();
        _windowProjectionRuntime.Dispose();
        _displaysPage.Dispose();
        _quickProjectionWindow?.Close();
        _trayIcon.Dispose();
        _windowSource?.RemoveHook(WindowHook);
        ClearHotKeyRegistrations();

        base.OnClosed(e);
    }

    private void NavigationList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedItem is NavigationItem selected)
        {
            PageHost.Content = selected.Page;
        }
    }

    private nint WindowHook(nint windowHandle, int message, nint wordParameter, nint longParameter, ref bool handled)
    {
        if (DisplayChangeMessage.RequiresThemeRefresh(message))
        {
            _themeService.RefreshSystemTheme();
        }

        if (DisplayChangeMessage.RequiresRefresh(message))
        {
            ScheduleDisplayRefresh();
        }

        if (message != GlobalHotKeyRegistration.HotKeyMessage)
        {
            return 0;
        }

        if (_registeredHotKeys.TryGetValue((int)wordParameter, out var hotKey))
        {
            handled = true;
            RunHotKey(hotKey);
        }

        return 0;
    }

    private void ScheduleDisplayRefresh()
    {
        _displayRefreshTimer.Stop();
        _displayRefreshTimer.Start();
    }

    private void DisplayRefreshTimer_OnTick(object? sender, EventArgs e)
    {
        _displayRefreshTimer.Stop();
        RefreshDisplays();
    }

    private void RefreshDisplays()
    {
        try
        {
            _displayProfileService.Refresh();
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException)
        {
            _diagnostics.Write("display.refresh.failed", exception.GetType().Name, always: true);
            _displaysPage.ShowRefreshFailure(exception.Message);
        }
    }

    private void DisplayProfileService_OnChanged(object? sender, EventArgs e)
    {
        RebuildHotKeys();
        _displaysPage.Refresh();
        _projectionPage.Refresh();
        _applicationProjectionPage.Refresh();
        _aboutPage.Refresh();
    }

    private void DisplayProfileService_OnTopologyChanged(object? sender, EventArgs e)
    {
        DisplayRecoveryController.HandleDisplaysChanged(
            _generalSettings.Current.RecallOffscreenWindows,
            () =>
            {
                var result = _offscreenWindowRecallService.Recall(_windowHandle);
                if (result.Status != OffscreenWindowRecallStatus.Completed || result.FailedCount > 0)
                {
                    _diagnostics.Write(
                        "window.recall.incomplete",
                        $"Status={result.Status};FailedCount={result.FailedCount}",
                        always: true);
                }
            });
    }

    private void ApplicationProjectionRuntime_OnProjectionCompleted(
        object? sender,
        ApplicationProjectionRuntimeResult result)
    {
        if (result.Status == ApplicationProjectionRuntimeStatus.Failed)
        {
            _diagnostics.Write("application.projection.failed", "Window move failed", always: true);
        }

        _applicationProjectionPage.ShowRuntimeResult(result);
    }

    private void ApplicationProjectionSettings_OnChanged(object? sender, EventArgs e)
    {
        _applicationProjectionPage.Refresh();
        _trayIcon.SetApplicationProjectionEnabled(_applicationProjectionSettings.Current.IsEnabled);
    }

    private void WindowProjectionRuntime_OnProjectionCompleted(
        object? sender,
        WindowProjectionRuntimeResult result)
    {
        if (result.Status == WindowProjectionRuntimeStatus.Failed)
        {
            _diagnostics.Write("window.projection.failed", "Window move failed", always: true);
        }

        _projectionPage.ShowRuntimeResult(result);
    }

    private void HotKeySettings_OnChanged(object? sender, EventArgs e)
    {
        foreach (var hotKey in HotKeys.Where(item => item.Definition is not null))
        {
            hotKey.SetGesture(_hotKeySettings.Current.Get(hotKey.Definition!.Id));
        }

        if (_windowHandle != 0)
        {
            RegisterConfiguredHotKeys();
        }
    }

    private void RegisterConfiguredHotKeys()
    {
        ClearHotKeyRegistrations();
        var registrationId = FirstDynamicHotKeyId;
        foreach (var hotKey in HotKeys)
        {
            RegisterHotKey(registrationId++, hotKey);
        }
    }

    private void ClearHotKeyRegistrations()
    {
        foreach (var registration in _hotKeyRegistrations)
        {
            registration.Dispose();
        }

        _hotKeyRegistrations.Clear();
        _registeredHotKeys.Clear();
    }

    private void RegisterHotKey(int registrationId, HotKeyStatusViewModel status)
    {
        if (status.Gesture is not { } gesture)
        {
            status.SetGesture(null);
            return;
        }

        try
        {
            _hotKeyRegistrations.Add(GlobalHotKeyRegistration.Register(_windowHandle, registrationId, gesture));
            _registeredHotKeys[registrationId] = status;
            status.Update(isAvailable: true);
        }
        catch (Win32Exception)
        {
            status.Update(isAvailable: false);
            _diagnostics.Write("hotkey.registration.conflict", status.Definition?.Id ?? "unknown", always: true);
        }
    }

    private void RunHotKey(HotKeyStatusViewModel hotKey)
    {
        if (hotKey.Definition is null)
        {
            return;
        }

        try
        {
            var result = _hotKeyExecutor.Execute(hotKey.Definition, _windowHandle);
            hotKey.UpdateStatus(result.StatusMessage);
            if (!result.Succeeded)
            {
                _diagnostics.Write("hotkey.execute.unsuccessful", hotKey.Definition.Id, always: true);
            }
        }
        catch (Exception exception) when (
            exception is Win32Exception or IOException or UnauthorizedAccessException or ArgumentException)
        {
            _diagnostics.Write("hotkey.execute.failed", hotKey.Definition.Id, always: true);
            hotKey.UpdateStatus("执行失败");
        }
    }

    private void RebuildHotKeys()
    {
        var definitions = HotKeyCatalog.CreateForDisplays(
            _displayProfileService.States.Select(state => new HotKeyDisplayTarget(
                state.Profile.StableId,
                state.DisplayName,
                state.IsConnected)));
        HotKeys.Clear();
        foreach (var definition in definitions)
        {
            HotKeys.Add(new HotKeyStatusViewModel(definition, _hotKeySettings.Current.Get(definition.Id)));
        }

        if (_windowHandle != 0)
        {
            RegisterConfiguredHotKeys();
        }
    }

    private void HotKeyExecutor_OnProjectionPanelRequested(
        object? sender,
        WindowProjectionCandidate candidate)
    {
        _quickProjectionWindow?.Close();
        var displays = _displayProfileService.ApplyNames(_hotKeyExecutor.ReadProjectionDisplays());
        _quickProjectionWindow = new QuickProjectionWindow(
            candidate,
            displays,
            _hotKeyExecutor.ResolveProjectionDefaultTargetId(candidate),
            _windowProjectionSettings.Current.DefaultLayout)
        {
            Owner = this,
        };
        _quickProjectionWindow.ProjectionRequested += QuickProjectionWindow_OnProjectionRequested;
        _quickProjectionWindow.Closed += (_, _) => _quickProjectionWindow = null;
        _quickProjectionWindow.Show();
        _quickProjectionWindow.Activate();
    }

    private void QuickProjectionWindow_OnProjectionRequested(
        object? sender,
        QuickProjectionRequest request)
    {
        if (_quickProjectionWindow is null)
        {
            return;
        }

        try
        {
            var result = _hotKeyExecutor.Project(
                _quickProjectionWindow.Candidate,
                request.Layout,
                request.TargetDisplayId);
            if (result.Status == WindowProjectionShortcutStatus.Moved)
            {
                _quickProjectionWindow.Close();
            }
            else
            {
                _quickProjectionWindow.ShowError("目标显示器或窗口当前不可用。");
            }
        }
        catch (Exception exception) when (
            exception is Win32Exception or UnauthorizedAccessException or ArgumentException)
        {
            _diagnostics.Write("window.projection.failed", exception.GetType().Name, always: true);
            _quickProjectionWindow.ShowError("窗口投放失败，可能是权限或窗口状态限制。");
        }
    }

    private void HotKeyExecutor_OnSettingsRequested(object? sender, EventArgs e)
        => ShowAndActivate();

    public void ShowAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
    }

    public void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    private ClosePromptResult? ShowClosePrompt()
    {
        var dialog = new CloseBehaviorDialog { Owner = this };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    private void BehaviorPage_OnRestartElevatedRequested(object? sender, EventArgs e)
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            _behaviorPage.ShowElevationResult(ElevationRestartResult.Failed);
            return;
        }

        var arguments = Environment.GetCommandLineArgs()
            .Skip(1)
            .Where(argument => !string.Equals(argument, "--background", StringComparison.OrdinalIgnoreCase))
            .Where(argument => !string.Equals(argument, "--elevation-restart", StringComparison.OrdinalIgnoreCase))
            .Append("--elevation-restart")
            .ToArray();
        var result = _processElevation.RestartElevated(executable, arguments);
        if (result == ElevationRestartResult.Started)
        {
            ExitApplication();
            return;
        }

        _behaviorPage.ShowElevationResult(result);
    }

    private void TrayIcon_OnOpenRequested(object? sender, EventArgs e) =>
        Dispatcher.Invoke(ShowAndActivate);

    private void TrayIcon_OnToggleApplicationProjectionRequested(object? sender, EventArgs e) =>
        Dispatcher.Invoke(() => RunTrayAction(HotKeyCommand.ToggleApplicationProjection));

    private void TrayIcon_OnRecallRequested(object? sender, EventArgs e) =>
        Dispatcher.Invoke(() => RunTrayAction(HotKeyCommand.RecallOffscreenWindows));

    private void TrayIcon_OnExitRequested(object? sender, EventArgs e) =>
        Dispatcher.Invoke(ExitApplication);

    private void RunTrayAction(HotKeyCommand command)
    {
        try
        {
            _ = _hotKeyExecutor.Execute(HotKeyCatalog.Get(command), _windowHandle);
        }
        catch (Exception exception) when (
            exception is Win32Exception or IOException or UnauthorizedAccessException or ArgumentException)
        {
            _diagnostics.Write("tray.action.failed", command.ToString(), always: true);
        }
    }

    private void Localization_OnChanged(object? sender, EventArgs e)
    {
        _trayIcon.RefreshText();
        _behaviorPage.Refresh();
        _aboutPage.Refresh();
    }

    private sealed record NavigationItem(string Label, string Icon, UserControl Page);

}
