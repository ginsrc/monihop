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
using MoniHop.Desktop.HotKeys;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.Views;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.HotKeys;
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
    private readonly DispatcherTimer _displayRefreshTimer;
    private HwndSource? _windowSource;
    private nint _windowHandle;
    private QuickProjectionWindow? _quickProjectionWindow;

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
            installedApplicationCatalog);
        _projectionPage = new ProjectionPage(_windowProjectionSettings, _displayProfileService);
        _displayProfileService.Changed += DisplayProfileService_OnChanged;
        _hotKeySettings.Changed += HotKeySettings_OnChanged;
        _applicationProjectionSettings.Changed += ApplicationProjectionSettings_OnChanged;
        _hotKeyExecutor.ProjectionPanelRequested += HotKeyExecutor_OnProjectionPanelRequested;
        _hotKeyExecutor.SettingsRequested += HotKeyExecutor_OnSettingsRequested;
        _applicationProjectionRuntime.ProjectionCompleted += ApplicationProjectionRuntime_OnProjectionCompleted;
        _windowProjectionRuntime.ProjectionCompleted += WindowProjectionRuntime_OnProjectionCompleted;
        _navigationItems =
        [
            new("显示器", "\uE7F4", _displaysPage),
            new("窗口投放", "\uE8A7", _projectionPage),
            new("应用投放", "\uE8FD", _applicationProjectionPage),
            new("快捷键", "\uE765", new HotKeysPage(HotKeys, _hotKeySettings)),
            new("行为与恢复", "\uE713", new BehaviorPage()),
            new("关于与诊断", "\uE946", new AboutPage(paths)),
        ];

        NavigationList.ItemsSource = _navigationItems;
        NavigationList.SelectedIndex = 0;
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

    protected override void OnClosed(EventArgs e)
    {
        _displayRefreshTimer.Stop();
        _displayProfileService.Changed -= DisplayProfileService_OnChanged;
        _hotKeySettings.Changed -= HotKeySettings_OnChanged;
        _applicationProjectionSettings.Changed -= ApplicationProjectionSettings_OnChanged;
        _hotKeyExecutor.ProjectionPanelRequested -= HotKeyExecutor_OnProjectionPanelRequested;
        _hotKeyExecutor.SettingsRequested -= HotKeyExecutor_OnSettingsRequested;
        _applicationProjectionRuntime.ProjectionCompleted -= ApplicationProjectionRuntime_OnProjectionCompleted;
        _windowProjectionRuntime.ProjectionCompleted -= WindowProjectionRuntime_OnProjectionCompleted;
        _applicationProjectionRuntime.Dispose();
        _windowProjectionRuntime.Dispose();
        _displaysPage.Dispose();
        _quickProjectionWindow?.Close();
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
        if (DisplayChangeMessage.RequiresRefresh(message))
        {
            ScheduleDisplayRefresh();
            return 0;
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
            _displaysPage.ShowRefreshFailure(exception.Message);
        }
    }

    private void DisplayProfileService_OnChanged(object? sender, EventArgs e)
    {
        RebuildHotKeys();
        _displaysPage.Refresh();
        _projectionPage.Refresh();
        _applicationProjectionPage.Refresh();
    }

    private void ApplicationProjectionRuntime_OnProjectionCompleted(
        object? sender,
        ApplicationProjectionRuntimeResult result) =>
        _applicationProjectionPage.ShowRuntimeResult(result);

    private void ApplicationProjectionSettings_OnChanged(object? sender, EventArgs e) =>
        _applicationProjectionPage.Refresh();

    private void WindowProjectionRuntime_OnProjectionCompleted(
        object? sender,
        WindowProjectionRuntimeResult result) =>
        _projectionPage.ShowRuntimeResult(result);

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
            hotKey.UpdateStatus(_hotKeyExecutor.Execute(hotKey.Definition, _windowHandle).StatusMessage);
        }
        catch (Exception exception) when (
            exception is Win32Exception or IOException or UnauthorizedAccessException or ArgumentException)
        {
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
            _quickProjectionWindow.ShowError("窗口投放失败，可能是权限或窗口状态限制。");
        }
    }

    private void HotKeyExecutor_OnSettingsRequested(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
    }

    private sealed record NavigationItem(string Label, string Icon, UserControl Page);

}
