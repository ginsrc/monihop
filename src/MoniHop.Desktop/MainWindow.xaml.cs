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
    private readonly CursorSwitchService _cursorSwitchService;
    private readonly WindowSwitchService _windowSwitchService;
    private readonly List<GlobalHotKeyRegistration> _hotKeyRegistrations = [];
    private readonly IReadOnlyList<NavigationItem> _navigationItems;
    private readonly DisplayProfileService _displayProfileService;
    private readonly DisplaysPage _displaysPage;
    private readonly ProjectionPage _projectionPage;
    private readonly ApplicationProjectionPage _applicationProjectionPage;
    private readonly ApplicationProjectionRuntime _applicationProjectionRuntime;
    private readonly WindowProjectionRuntime _windowProjectionRuntime;
    private readonly WindowProjectionSettingsService _windowProjectionSettings;
    private readonly DispatcherTimer _displayRefreshTimer;
    private HwndSource? _windowSource;
    private nint _windowHandle;

    public MainWindow(
        IReadOnlyList<DisplaySnapshot> displays,
        CursorSwitchService cursorSwitchService,
        WindowSwitchService windowSwitchService,
        DisplayProfileService displayProfileService,
        ApplicationProjectionSettingsService applicationProjectionSettings,
        IApplicationWindowController applicationWindowController,
        IInstalledApplicationCatalog installedApplicationCatalog,
        ApplicationProjectionRuntime applicationProjectionRuntime,
        WindowProjectionSettingsService windowProjectionSettings,
        WindowProjectionRuntime windowProjectionRuntime,
        MoniHopPaths paths)
    {
        ArgumentNullException.ThrowIfNull(displays);
        _cursorSwitchService = cursorSwitchService ?? throw new ArgumentNullException(nameof(cursorSwitchService));
        _windowSwitchService = windowSwitchService ?? throw new ArgumentNullException(nameof(windowSwitchService));
        _displayProfileService = displayProfileService ?? throw new ArgumentNullException(nameof(displayProfileService));
        ArgumentNullException.ThrowIfNull(applicationProjectionSettings);
        ArgumentNullException.ThrowIfNull(applicationWindowController);
        ArgumentNullException.ThrowIfNull(installedApplicationCatalog);
        _applicationProjectionRuntime = applicationProjectionRuntime ?? throw new ArgumentNullException(nameof(applicationProjectionRuntime));
        _windowProjectionSettings = windowProjectionSettings ?? throw new ArgumentNullException(nameof(windowProjectionSettings));
        _windowProjectionRuntime = windowProjectionRuntime ?? throw new ArgumentNullException(nameof(windowProjectionRuntime));
        ArgumentNullException.ThrowIfNull(paths);

        CursorHotKey = new HotKeyStatusViewModel("鼠标切到下一屏", "Ctrl + Alt + M");
        WindowPreviousHotKey = new HotKeyStatusViewModel("当前窗口移到上一屏", "Ctrl + Alt + Shift + Left");
        WindowNextHotKey = new HotKeyStatusViewModel("当前窗口移到下一屏", "Ctrl + Alt + Shift + Right");
        HotKeys = [CursorHotKey, WindowPreviousHotKey, WindowNextHotKey];

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
        _applicationProjectionRuntime.ProjectionCompleted += ApplicationProjectionRuntime_OnProjectionCompleted;
        _windowProjectionRuntime.ProjectionCompleted += WindowProjectionRuntime_OnProjectionCompleted;
        _navigationItems =
        [
            new("显示器", "\uE7F4", _displaysPage),
            new("窗口投放", "\uE8A7", _projectionPage),
            new("应用投放", "\uE8FD", _applicationProjectionPage),
            new("快捷键", "\uE765", new HotKeysPage(HotKeys)),
            new("行为与恢复", "\uE713", new BehaviorPage()),
            new("关于与诊断", "\uE946", new AboutPage(paths)),
        ];

        NavigationList.ItemsSource = _navigationItems;
        NavigationList.SelectedIndex = 0;
    }

    public ObservableCollection<HotKeyStatusViewModel> HotKeys { get; }

    public HotKeyStatusViewModel CursorHotKey { get; }

    public HotKeyStatusViewModel WindowPreviousHotKey { get; }

    public HotKeyStatusViewModel WindowNextHotKey { get; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(_windowHandle);
        _windowSource.AddHook(WindowHook);

        RegisterHotKey(GlobalHotKeyAction.CursorSwitch, CursorHotKey);
        RegisterHotKey(GlobalHotKeyAction.WindowPrevious, WindowPreviousHotKey);
        RegisterHotKey(GlobalHotKeyAction.WindowNext, WindowNextHotKey);
    }

    protected override void OnClosed(EventArgs e)
    {
        _displayRefreshTimer.Stop();
        _displayProfileService.Changed -= DisplayProfileService_OnChanged;
        _applicationProjectionRuntime.ProjectionCompleted -= ApplicationProjectionRuntime_OnProjectionCompleted;
        _windowProjectionRuntime.ProjectionCompleted -= WindowProjectionRuntime_OnProjectionCompleted;
        _applicationProjectionRuntime.Dispose();
        _windowProjectionRuntime.Dispose();
        _displaysPage.Dispose();
        _windowSource?.RemoveHook(WindowHook);
        foreach (var registration in _hotKeyRegistrations)
        {
            registration.Dispose();
        }

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

        switch ((int)wordParameter)
        {
            case GlobalHotKeyRegistration.CursorSwitchId:
                handled = true;
                RunCursorSwitch();
                break;
            case GlobalHotKeyRegistration.WindowPreviousId:
                handled = true;
                RunWindowSwitch(DisplayDirection.Previous, WindowPreviousHotKey);
                break;
            case GlobalHotKeyRegistration.WindowNextId:
                handled = true;
                RunWindowSwitch(DisplayDirection.Next, WindowNextHotKey);
                break;
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
        _displaysPage.Refresh();
        _projectionPage.Refresh();
        _applicationProjectionPage.Refresh();
    }

    private void ApplicationProjectionRuntime_OnProjectionCompleted(
        object? sender,
        ApplicationProjectionRuntimeResult result) =>
        _applicationProjectionPage.ShowRuntimeResult(result);

    private void WindowProjectionRuntime_OnProjectionCompleted(
        object? sender,
        WindowProjectionRuntimeResult result) =>
        _projectionPage.ShowRuntimeResult(result);

    private void RegisterHotKey(GlobalHotKeyAction action, HotKeyStatusViewModel status)
    {
        try
        {
            _hotKeyRegistrations.Add(GlobalHotKeyRegistration.Register(_windowHandle, action));
            status.Update(isAvailable: true);
        }
        catch (Win32Exception)
        {
            status.Update(isAvailable: false);
        }
    }

    private void RunCursorSwitch()
    {
        try
        {
            CursorHotKey.UpdateStatus(_cursorSwitchService.SwitchNext() switch
            {
                CursorSwitchResult.Moved => "已切换",
                CursorSwitchResult.NoTarget => "仅连接一块显示器",
                _ => "切换失败",
            });
        }
        catch (Win32Exception)
        {
            CursorHotKey.UpdateStatus("切换失败");
        }
    }

    private void RunWindowSwitch(DisplayDirection direction, HotKeyStatusViewModel status)
    {
        try
        {
            status.UpdateStatus(_windowSwitchService.Switch(direction, _windowHandle) switch
            {
                WindowSwitchResult.Moved => "已移动",
                WindowSwitchResult.NoTarget => "仅连接一块显示器",
                WindowSwitchResult.NoWindow => "当前窗口不可移动",
                _ => "移动失败",
            });
        }
        catch (Win32Exception)
        {
            status.UpdateStatus("移动失败");
        }
    }

    private sealed record NavigationItem(string Label, string Icon, UserControl Page);

}
