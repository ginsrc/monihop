using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using MoniHop.Core.Displays;
using MoniHop.Core.Windows;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.HotKeys;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop;

public partial class MainWindow : Window
{
    private readonly CursorSwitchService _cursorSwitchService;
    private readonly WindowSwitchService _windowSwitchService;
    private readonly List<GlobalHotKeyRegistration> _hotKeyRegistrations = [];
    private HwndSource? _windowSource;
    private nint _windowHandle;

    public MainWindow(
        IReadOnlyList<DisplaySnapshot> displays,
        CursorSwitchService cursorSwitchService,
        WindowSwitchService windowSwitchService)
    {
        ArgumentNullException.ThrowIfNull(displays);
        _cursorSwitchService = cursorSwitchService ??
            throw new ArgumentNullException(nameof(cursorSwitchService));
        _windowSwitchService = windowSwitchService ??
            throw new ArgumentNullException(nameof(windowSwitchService));

        Displays = new ObservableCollection<DisplayItem>(
            displays.Select((display, index) => new DisplayItem(
                $"显示器 {index + 1}",
                display.DeviceName,
                $"{display.Bounds.Width} × {display.Bounds.Height}",
                display.IsPrimary ? Visibility.Visible : Visibility.Collapsed)));

        DataContext = this;
        InitializeComponent();
    }

    public ObservableCollection<DisplayItem> Displays { get; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(_windowHandle);
        _windowSource.AddHook(WindowHook);

        RegisterHotKey(GlobalHotKeyAction.CursorSwitch, CursorHotKeyStatusText);
        RegisterHotKey(GlobalHotKeyAction.WindowPrevious, WindowPreviousHotKeyStatusText);
        RegisterHotKey(GlobalHotKeyAction.WindowNext, WindowNextHotKeyStatusText);
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

    private nint WindowHook(
        nint windowHandle,
        int message,
        nint wordParameter,
        nint longParameter,
        ref bool handled)
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
                RunWindowSwitch(DisplayDirection.Previous, WindowPreviousHotKeyStatusText);
                break;
            case GlobalHotKeyRegistration.WindowNextId:
                handled = true;
                RunWindowSwitch(DisplayDirection.Next, WindowNextHotKeyStatusText);
                break;
        }

        return 0;
    }

    private void RegisterHotKey(GlobalHotKeyAction action, TextBlock statusText)
    {
        try
        {
            _hotKeyRegistrations.Add(GlobalHotKeyRegistration.Register(_windowHandle, action));
            statusText.Text = "已启用";
        }
        catch (Win32Exception)
        {
            statusText.Text = "快捷键不可用";
        }
    }

    private void RunCursorSwitch()
    {
        try
        {
            CursorHotKeyStatusText.Text = _cursorSwitchService.SwitchNext() switch
            {
                CursorSwitchResult.Moved => "已切换",
                CursorSwitchResult.NoTarget => "仅连接一块显示器",
                _ => "切换失败",
            };
        }
        catch (Win32Exception)
        {
            CursorHotKeyStatusText.Text = "切换失败";
        }
    }

    private void RunWindowSwitch(DisplayDirection direction, TextBlock statusText)
    {
        try
        {
            statusText.Text = _windowSwitchService.Switch(direction, _windowHandle) switch
            {
                WindowSwitchResult.Moved => "已移动",
                WindowSwitchResult.NoTarget => "仅连接一块显示器",
                WindowSwitchResult.NoWindow => "当前窗口不可移动",
                _ => "移动失败",
            };
        }
        catch (Win32Exception)
        {
            statusText.Text = "移动失败";
        }
    }

    public sealed record DisplayItem(
        string DisplayName,
        string DeviceName,
        string ResolutionText,
        Visibility PrimaryVisibility);
}
