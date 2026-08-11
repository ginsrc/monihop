using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using MoniHop.Core.Displays;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop;

public partial class MainWindow : Window
{
    private readonly CursorSwitchService _cursorSwitchService;
    private GlobalHotKeyRegistration? _hotKeyRegistration;
    private HwndSource? _windowSource;

    public MainWindow(
        IReadOnlyList<DisplaySnapshot> displays,
        CursorSwitchService cursorSwitchService)
    {
        ArgumentNullException.ThrowIfNull(displays);
        _cursorSwitchService = cursorSwitchService ??
            throw new ArgumentNullException(nameof(cursorSwitchService));

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

        var windowHandle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(windowHandle);
        _windowSource.AddHook(WindowHook);

        try
        {
            _hotKeyRegistration = GlobalHotKeyRegistration.Register(windowHandle);
            HotKeyStatusText.Text = "已启用";
        }
        catch (Win32Exception)
        {
            HotKeyStatusText.Text = "快捷键不可用";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _windowSource?.RemoveHook(WindowHook);
        _hotKeyRegistration?.Dispose();
        base.OnClosed(e);
    }

    private nint WindowHook(
        nint windowHandle,
        int message,
        nint wordParameter,
        nint longParameter,
        ref bool handled)
    {
        if (message != GlobalHotKeyRegistration.HotKeyMessage ||
            wordParameter != GlobalHotKeyRegistration.CursorSwitchId)
        {
            return 0;
        }

        handled = true;

        try
        {
            HotKeyStatusText.Text = _cursorSwitchService.SwitchNext() switch
            {
                CursorSwitchResult.Moved => "已启用",
                CursorSwitchResult.NoTarget => "仅连接一块显示器",
                _ => "切换失败",
            };
        }
        catch (Win32Exception)
        {
            HotKeyStatusText.Text = "切换失败";
        }

        return 0;
    }

    public sealed record DisplayItem(
        string DisplayName,
        string DeviceName,
        string ResolutionText,
        Visibility PrimaryVisibility);
}
