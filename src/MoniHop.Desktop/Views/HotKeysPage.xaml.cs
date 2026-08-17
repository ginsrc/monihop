using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Notifications;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Views;

public partial class HotKeysPage : UserControl
{
    private readonly HotKeySettingsService _settingsService;
    private readonly GeneralSettingsService _generalSettings;
    private HotKeyStatusViewModel? _capturing;

    public HotKeysPage(
        ObservableCollection<HotKeyStatusViewModel> hotKeys,
        HotKeySettingsService settingsService,
        GeneralSettingsService generalSettings)
    {
        InitializeComponent();
        HotKeys = hotKeys ?? throw new ArgumentNullException(nameof(hotKeys));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _generalSettings = generalSettings ?? throw new ArgumentNullException(nameof(generalSettings));
        GroupedHotKeys = CollectionViewSource.GetDefaultView(HotKeys);
        GroupedHotKeys.GroupDescriptions.Add(new PropertyGroupDescription(nameof(HotKeyStatusViewModel.Category)));
        DataContext = this;
    }

    public ObservableCollection<HotKeyStatusViewModel> HotKeys { get; }

    public ICollectionView GroupedHotKeys { get; }

    private void CaptureShortcutButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: HotKeyStatusViewModel model } || !model.CanEdit)
        {
            return;
        }

        CancelCapture();
        _capturing = model;
        model.BeginCapture();
        CaptureNoticeText.Text = "请按下包含 Ctrl、Alt、Shift 或 Win 的组合键；Esc 取消，Backspace 清除。";
        CaptureNotice.Visibility = Visibility.Visible;
        Focus();
        Keyboard.Focus(this);
    }

    private void ClearShortcutButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HotKeyStatusViewModel { Definition: { } definition } model } && model.CanEdit)
        {
            SaveBinding(model, definition, gesture: null);
        }
    }

    private void ResetDefaultsButton_OnClick(object sender, RoutedEventArgs e)
    {
        CancelCapture();
        try
        {
            _settingsService.ResetDefaults();
            ShowNotice("已恢复三项默认快捷键。", isError: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowNotice("无法保存快捷键设置。", isError: true);
        }
    }

    private void HotKeysPage_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_capturing is not { Definition: { } definition } model)
        {
            return;
        }

        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            CancelCapture();
            return;
        }

        if (key is Key.Back or Key.Delete)
        {
            SaveBinding(model, definition, gesture: null);
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        if (!HotKeyGestureFormatter.TryCreate(key, Keyboard.Modifiers, out var gesture))
        {
            ShowNotice("快捷键必须包含 Ctrl、Alt、Shift 或 Win，并以非修饰键结束。", isError: true);
            return;
        }

        SaveBinding(model, definition, gesture);
    }

    private void SaveBinding(
        HotKeyStatusViewModel model,
        HotKeyActionDefinition definition,
        MoniHop.Windows.HotKeys.HotKeyGesture? gesture)
    {
        try
        {
            if (!_settingsService.TryUpdate(
                    definition,
                    gesture,
                    HotKeys.Select(item => item.Definition).OfType<HotKeyActionDefinition>().ToArray(),
                    out var error))
            {
                model.CancelCapture();
                model.UpdateStatus("冲突");
                ShowNotice(error ?? "无法保存快捷键。", isError: true);
                _capturing = null;
                return;
            }

            _capturing = null;
            ShowNotice(gesture is null ? "快捷键已清除。" : "快捷键已更新并立即生效。", isError: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            model.CancelCapture();
            _capturing = null;
            ShowNotice("无法保存快捷键设置。", isError: true);
        }
    }

    private void CancelCapture()
    {
        _capturing?.CancelCapture();
        _capturing = null;
        CaptureNotice.Visibility = Visibility.Collapsed;
    }

    private void ShowNotice(string message, bool isError)
    {
        if (!UserNotificationPolicy.ShouldShow(
                _generalSettings.Current.ShowSuccessNotifications,
                isError ? UserNotificationSeverity.Error : UserNotificationSeverity.Success))
        {
            CaptureNotice.Visibility = Visibility.Collapsed;
            return;
        }

        CaptureNoticeText.Text = message;
        CaptureNoticeText.Foreground = isError
            ? (System.Windows.Media.Brush)FindResource("ErrorBrush")
            : (System.Windows.Media.Brush)FindResource("BrandBrush");
        CaptureNotice.Background = isError
            ? (System.Windows.Media.Brush)FindResource("ErrorSubtleBrush")
            : (System.Windows.Media.Brush)FindResource("BrandSubtleBrush");
        CaptureNotice.Visibility = Visibility.Visible;
    }
}
