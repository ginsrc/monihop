using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;

namespace MoniHop.Desktop.WindowProjection;

public partial class WindowProjectionOverlay : Window, IWindowProjectionOverlay
{
    private const int ExtendedStyleIndex = -20;
    private const int ToolWindow = 0x00000080;
    private const int NoActivate = 0x08000000;
    private const int Transparent = 0x00000020;
    private readonly ObservableCollection<OverlayLayoutViewModel> _layouts = [];
    private readonly ObservableCollection<OverlayDisplayViewModel> _displays = [];
    private nint _handle;
    private int _animationVersion;
    private bool _disposed;

    public WindowProjectionOverlay()
    {
        InitializeComponent();
        LayoutList.ItemsSource = _layouts;
        DisplayList.ItemsSource = _displays;
        SourceInitialized += (_, _) =>
        {
            _handle = new WindowInteropHelper(this).Handle;
            var styles = GetWindowLongPtr(_handle, ExtendedStyleIndex).ToInt64();
            _ = SetWindowLongPtr(_handle, ExtendedStyleIndex, new nint(styles | ToolWindow | NoActivate | Transparent));
        };
    }

    public void ShowPortal(PixelRect bounds)
    {
        RunOnUi(() =>
        {
            _animationVersion++;
            CommandRoot.Visibility = Visibility.Collapsed;
            SetPortalOnlyGrid();
            PortalRoot.Visibility = Visibility.Visible;
            PortalRoot.Opacity = 0;
            PortalTranslate.Y = -4;
            ShowWindow(bounds);
            Animate(PortalRoot, UIElement.OpacityProperty, 0, 1, 90);
            Animate(PortalTranslate, TranslateTransform.YProperty, -4, 0, 90);
        });
    }

    public void ShowCommands(
        WindowProjectionCommandLayout command,
        ProjectionLayout defaultLayout,
        WindowProjectionHit hit)
    {
        RunOnUi(() =>
        {
            _animationVersion++;
            UpdateContent(command, defaultLayout, hit);
            var overlayBounds = new WindowProjectionPlanner().CalculateExpandedOverlayBounds(
                command.PortalBounds,
                command.PanelBounds);
            PortalRoot.Visibility = Visibility.Visible;
            PortalRoot.Opacity = 1;
            PortalTranslate.Y = 0;
            CommandRoot.Visibility = Visibility.Visible;
            CommandRoot.Opacity = 0;
            CommandContent.Opacity = 0;
            CommandScale.ScaleY = .9;
            SetExpandedGrid(command, overlayBounds);
            ShowWindow(overlayBounds);
            Animate(CommandRoot, UIElement.OpacityProperty, 0, 1, 160);
            Animate(CommandScale, ScaleTransform.ScaleYProperty, .9, 1, 160);
            Animate(CommandContent, UIElement.OpacityProperty, 0, 1, 120, 40);
        });
    }

    public void UpdateCommands(
        WindowProjectionCommandLayout command,
        ProjectionLayout defaultLayout,
        WindowProjectionHit hit) =>
        RunOnUi(() => UpdateContent(command, defaultLayout, hit));

    void IWindowProjectionOverlay.Hide()
    {
        RunOnUi(() =>
        {
            if (!IsVisible)
            {
                return;
            }

            var version = ++_animationVersion;
            var visibleElement = CommandRoot.Visibility == Visibility.Visible
                ? (UIElement)CommandRoot
                : PortalRoot;
            var animation = new DoubleAnimation(visibleElement.Opacity, 0, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
                FillBehavior = FillBehavior.Stop,
            };
            animation.Completed += (_, _) =>
            {
                if (version != _animationVersion)
                {
                    return;
                }

                visibleElement.Opacity = 0;
                base.Hide();
            };
            visibleElement.BeginAnimation(UIElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
        });
    }

    public void Dispose()
    {
        RunOnUi(() =>
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _animationVersion++;
            CommandRoot.BeginAnimation(UIElement.OpacityProperty, null);
            PortalRoot.BeginAnimation(UIElement.OpacityProperty, null);
            base.Close();
        });
    }

    private void RunOnUi(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _ = Dispatcher.BeginInvoke(action);
    }

    private void SetPortalOnlyGrid()
    {
        OverlayLeftColumn.Width = new GridLength(0);
        OverlayPortalColumn.Width = new GridLength(1, GridUnitType.Star);
        OverlayRightColumn.Width = new GridLength(0);
        OverlayPortalRow.Height = new GridLength(1, GridUnitType.Star);
        OverlayCommandRow.Height = new GridLength(0);
    }

    private void SetExpandedGrid(WindowProjectionCommandLayout command, PixelRect overlayBounds)
    {
        OverlayLeftColumn.Width = new GridLength(command.PortalBounds.Left - overlayBounds.Left, GridUnitType.Star);
        OverlayPortalColumn.Width = new GridLength(command.PortalBounds.Width, GridUnitType.Star);
        OverlayRightColumn.Width = new GridLength(overlayBounds.Right - command.PortalBounds.Right, GridUnitType.Star);
        OverlayPortalRow.Height = new GridLength(command.PortalBounds.Height, GridUnitType.Star);
        OverlayCommandRow.Height = new GridLength(command.PanelBounds.Height, GridUnitType.Star);
    }

    private void UpdateContent(
        WindowProjectionCommandLayout command,
        ProjectionLayout defaultLayout,
        WindowProjectionHit hit)
    {
        var selected = command.DisplayZones.First(zone => zone.IsSelected).TargetDisplay;
        DefaultTargetText.Text = selected.DisplayName;
        DefaultLayoutText.Text = LayoutLabel(defaultLayout);
        var defaultHighlighted = hit.Kind == WindowProjectionHitKind.DefaultDrop;
        DefaultDropCard.Background = Brush(defaultHighlighted ? "#DCE9FF" : "#F1F4F8");
        DefaultDropCard.BorderBrush = Brush(defaultHighlighted ? "#2563EB" : "#D5DAE2");
        if (defaultHighlighted)
        {
            Animate(DefaultDropCard, UIElement.OpacityProperty, .72, 1, 80);
        }

        if (_layouts.Count == 0)
        {
            foreach (var zone in command.LayoutZones)
            {
                _layouts.Add(new OverlayLayoutViewModel(zone.Layout, LayoutLabel(zone.Layout)));
            }
        }
        foreach (var item in _layouts)
        {
            item.IsHighlighted = hit.Kind == WindowProjectionHitKind.Layout && hit.Layout == item.Layout;
        }

        if (!DisplaySetMatches(command.DisplayZones))
        {
            _displays.Clear();
            for (var index = 0; index < command.DisplayZones.Count; index++)
            {
                var zone = command.DisplayZones[index];
                _displays.Add(new OverlayDisplayViewModel(
                    zone.TargetDisplay.StableId,
                    (index + 1).ToString(),
                    zone.TargetDisplay.DisplayName));
            }
        }
        foreach (var item in _displays)
        {
            item.IsSelected = command.DisplayZones.Any(zone =>
                zone.IsSelected && StringComparer.OrdinalIgnoreCase.Equals(zone.TargetDisplay.StableId, item.StableId));
            item.IsHighlighted = hit.Kind == WindowProjectionHitKind.Display &&
                StringComparer.OrdinalIgnoreCase.Equals(hit.TargetDisplay?.StableId, item.StableId);
        }
    }

    private bool DisplaySetMatches(IReadOnlyList<WindowProjectionDisplayZone> zones) =>
        _displays.Count == zones.Count && _displays.Select(item => item.StableId).SequenceEqual(
            zones.Select(zone => zone.TargetDisplay.StableId),
            StringComparer.OrdinalIgnoreCase);

    private void ShowWindow(PixelRect bounds)
    {
        if (!IsVisible)
        {
            base.Show();
        }

        _ = SetWindowPos(
            _handle,
            new nint(-1),
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            0x0010 | 0x0004 | 0x0020);
    }

    private static void Animate(
        DependencyObject target,
        DependencyProperty property,
        double from,
        double to,
        int durationMilliseconds,
        int delayMilliseconds = 0)
    {
        var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMilliseconds))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.HoldEnd,
        };
        switch (target)
        {
            case UIElement element:
                element.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
                break;
            case Animatable animatable:
                animatable.BeginAnimation(property, animation, HandoffBehavior.SnapshotAndReplace);
                break;
            default:
                throw new ArgumentException("Target does not support animation.", nameof(target));
        }
    }

    private static string LayoutLabel(ProjectionLayout layout) => layout switch
    {
        ProjectionLayout.KeepSize => "保持尺寸",
        ProjectionLayout.Maximized => "最大化",
        ProjectionLayout.LeftHalf => "左半屏",
        ProjectionLayout.RightHalf => "右半屏",
        _ => string.Empty,
    };

    private static SolidColorBrush Brush(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));

    private abstract class NotifyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    private sealed class OverlayLayoutViewModel(ProjectionLayout layout, string label) : NotifyViewModel
    {
        private bool _isHighlighted;
        public ProjectionLayout Layout { get; } = layout;
        public string Label { get; } = label;
        public bool IsHighlighted { get => _isHighlighted; set => Set(ref _isHighlighted, value); }
    }

    private sealed class OverlayDisplayViewModel(string stableId, string number, string name) : NotifyViewModel
    {
        private bool _isSelected;
        private bool _isHighlighted;
        public string StableId { get; } = stableId;
        public string Number { get; } = number;
        public string Name { get; } = name;
        public bool IsSelected { get => _isSelected; set => Set(ref _isSelected, value); }
        public bool IsHighlighted { get => _isHighlighted; set => Set(ref _isHighlighted, value); }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint windowHandle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
