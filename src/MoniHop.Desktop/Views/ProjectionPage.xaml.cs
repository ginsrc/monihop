using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Notifications;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.WindowProjection;

namespace MoniHop.Desktop.Views;

public partial class ProjectionPage : UserControl
{
    private readonly WindowProjectionSettingsService _settingsService;
    private readonly DisplayProfileService _displayProfileService;
    private readonly GeneralSettingsService _generalSettings;
    private readonly DispatcherTimer _toastTimer;
    private bool _isDragging;
    private bool _isApplyingState;
    private Point _dragOffset;

    public ProjectionPage(
        WindowProjectionSettingsService settingsService,
        DisplayProfileService displayProfileService,
        GeneralSettingsService generalSettings)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _displayProfileService = displayProfileService ?? throw new ArgumentNullException(nameof(displayProfileService));
        _generalSettings = generalSettings ?? throw new ArgumentNullException(nameof(generalSettings));
        InitializeComponent();
        _toastTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2.2),
            IsEnabled = false,
        };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            ActionToast.Visibility = Visibility.Collapsed;
        };
        Loaded += (_, _) => Refresh();
        _settingsService.Changed += (_, _) => Refresh();
    }

    public void ShowRuntimeResult(WindowProjectionRuntimeResult result)
    {
        void Show()
        {
            var failed = result.Status == WindowProjectionRuntimeStatus.Failed;
            if (!UserNotificationPolicy.ShouldShow(
                    _generalSettings.Current.ShowSuccessNotifications,
                    failed ? UserNotificationSeverity.Error : UserNotificationSeverity.Success))
            {
                return;
            }

            ToastText.Text = failed
                ? "窗口投放失败：可能是权限不足或窗口已失效。"
                : "窗口已投放。";
            ToastIndicator.Fill = (Brush)FindResource(failed ? "ErrorBrush" : "SuccessBrush");
            ActionToast.Visibility = Visibility.Visible;
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        if (Dispatcher.CheckAccess())
        {
            Show();
        }
        else
        {
            _ = Dispatcher.BeginInvoke(Show);
        }
    }

    public void Refresh()
    {
        _isApplyingState = true;
        var settings = _settingsService.Current;
        EnabledToggle.IsChecked = settings.IsEnabled;
        TriggerModeCombo.SelectedIndex = settings.TriggerMode == WindowProjectionTriggerMode.Immediate ? 1 : 0;
        var connected = _displayProfileService.States.Where(state => state.IsConnected).ToArray();
        var targets = new List<DisplayTargetViewModel>
        {
            new(null, "下一块显示器（默认）", true),
        };
        for (var index = 0; index < connected.Length; index++)
        {
            targets.Add(new DisplayTargetViewModel(
                connected[index].Profile.StableId,
                $"屏幕 {index + 1} · {connected[index].DisplayName}",
                true));
        }
        var configuredTarget = settings.DefaultTargetDisplayId;
        var targetAvailable = configuredTarget is null || targets.Any(target =>
            StringComparer.OrdinalIgnoreCase.Equals(target.StableId, configuredTarget));
        if (!targetAvailable)
        {
            targets.Add(new DisplayTargetViewModel(configuredTarget, "目标不可用（保留配置）", false));
        }
        DefaultTargetCombo.ItemsSource = targets;
        DefaultTargetCombo.SelectedItem = targets.First(target =>
            StringComparer.OrdinalIgnoreCase.Equals(target.StableId, configuredTarget));
        MissingTargetNotice.Visibility = targetAvailable ? Visibility.Collapsed : Visibility.Visible;

        var layouts = new[]
        {
            new ProjectionLayoutViewModel(ProjectionLayout.KeepSize, "保持尺寸"),
            new ProjectionLayoutViewModel(ProjectionLayout.Maximized, "最大化"),
            new ProjectionLayoutViewModel(ProjectionLayout.LeftHalf, "左半屏"),
            new ProjectionLayoutViewModel(ProjectionLayout.RightHalf, "右半屏"),
        };
        DefaultLayoutCombo.ItemsSource = layouts;
        DefaultLayoutCombo.SelectedItem = layouts.First(layout => layout.Layout == settings.DefaultLayout);
        EditPositionButton.Content = settings.IsPositionLocked ? "调整位置" : "锁定位置";
        UpdateMarker(settings.RelativePosition);
        _isApplyingState = false;
    }

    private void EnabledToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!_isApplyingState && IsInitialized && EnabledToggle.IsChecked is bool value) _settingsService.UpdateEnabled(value);
    }

    private void TriggerModeCombo_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isApplyingState && TriggerModeCombo.SelectedItem is ComboBoxItem item && Enum.TryParse<WindowProjectionTriggerMode>(item.Tag?.ToString(), out var mode)) _settingsService.UpdateTriggerMode(mode);
    }

    private void DefaultTargetCombo_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isApplyingState && DefaultTargetCombo.SelectedItem is DisplayTargetViewModel target)
        {
            _settingsService.UpdateDefaultTarget(target.StableId);
        }
    }

    private void DefaultLayoutCombo_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isApplyingState && DefaultLayoutCombo.SelectedItem is ProjectionLayoutViewModel layout)
        {
            _settingsService.UpdateDefaultLayout(layout.Layout);
        }
    }

    private void EditPositionButton_OnClick(object sender, RoutedEventArgs e) => _settingsService.SetPositionEditing(_settingsService.Current.IsPositionLocked);

    private void ResetPositionButton_OnClick(object sender, RoutedEventArgs e) => _settingsService.ResetPosition();

    private void PositionCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settingsService.Current.IsPositionLocked) return;
        _isDragging = true;
        _dragOffset = e.GetPosition(PositionMarker);
        PositionCanvas.CaptureMouse();
    }

    private void PositionCanvas_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var point = e.GetPosition(PositionCanvas);
        var left = Math.Clamp(point.X - _dragOffset.X, 0, Math.Max(0, PositionCanvas.ActualWidth - PositionMarker.ActualWidth));
        Canvas.SetLeft(PositionMarker, left);
        Canvas.SetTop(PositionMarker, 0);
    }

    private void PositionCanvas_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        PositionCanvas.ReleaseMouseCapture();
        var left = Canvas.GetLeft(PositionMarker);
        var x = (left + PositionMarker.ActualWidth / 2) / Math.Max(1, PositionCanvas.ActualWidth);
        _settingsService.UpdatePosition(new RelativePosition(x, 0));
    }

    private void UpdateMarker(RelativePosition position)
    {
        if (PositionCanvas.ActualWidth <= 0 || PositionCanvas.ActualHeight <= 0) return;
        Canvas.SetLeft(PositionMarker, Math.Clamp(position.X * PositionCanvas.ActualWidth - PositionMarker.Width / 2, 0, Math.Max(0, PositionCanvas.ActualWidth - PositionMarker.Width)));
        Canvas.SetTop(PositionMarker, 0);
    }

    private void PositionCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateMarker(_settingsService.Current.RelativePosition);
}
