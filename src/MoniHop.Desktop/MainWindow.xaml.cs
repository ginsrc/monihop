using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using MoniHop.Core.Displays;
using MoniHop.Core.Windows;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Views;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.HotKeys;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop;

public partial class MainWindow : Window
{
    private readonly CursorSwitchService _cursorSwitchService;
    private readonly WindowSwitchService _windowSwitchService;
    private readonly List<GlobalHotKeyRegistration> _hotKeyRegistrations = [];
    private readonly IReadOnlyList<NavigationItem> _navigationItems;
    private HwndSource? _windowSource;
    private nint _windowHandle;

    public MainWindow(
        IReadOnlyList<DisplaySnapshot> displays,
        CursorSwitchService cursorSwitchService,
        WindowSwitchService windowSwitchService)
    {
        ArgumentNullException.ThrowIfNull(displays);
        _cursorSwitchService = cursorSwitchService ?? throw new ArgumentNullException(nameof(cursorSwitchService));
        _windowSwitchService = windowSwitchService ?? throw new ArgumentNullException(nameof(windowSwitchService));

        CursorHotKey = new HotKeyStatusViewModel("鼠标切到下一屏", "Ctrl + Alt + M");
        WindowPreviousHotKey = new HotKeyStatusViewModel("当前窗口移到上一屏", "Ctrl + Alt + Shift + Left");
        WindowNextHotKey = new HotKeyStatusViewModel("当前窗口移到下一屏", "Ctrl + Alt + Shift + Right");
        HotKeys = [CursorHotKey, WindowPreviousHotKey, WindowNextHotKey];

        InitializeComponent();

        _navigationItems =
        [
            new("显示器", "\uE7F4", new DisplaysPage(displays)),
            new("窗口投放", "\uE8A7", new ProjectionPage()),
            new("应用投放", "\uE8FD", new ApplicationProjectionPage(displays)),
            new("快捷键", "\uE765", new HotKeysPage(HotKeys)),
            new("行为与恢复", "\uE713", new BehaviorPage()),
            new("关于与诊断", "\uE946", new AboutPage()),
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
