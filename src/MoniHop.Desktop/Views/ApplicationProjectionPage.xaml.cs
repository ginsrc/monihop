using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Desktop.ApplicationProjection;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Notifications;
using MoniHop.Desktop.Settings;
using MoniHop.Windows.ApplicationProjection;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Views;

public partial class ApplicationProjectionPage : UserControl
{
    private readonly ApplicationProjectionSettingsService _settingsService;
    private readonly DisplayProfileService _displayProfileService;
    private readonly IApplicationWindowController _windowController;
    private readonly IInstalledApplicationCatalog _installedApplicationCatalog;
    private readonly GeneralSettingsService _generalSettings;
    private readonly DispatcherTimer _toastTimer;
    private string? _primaryDisplayId;
    private bool _isApplyingState;

    public ApplicationProjectionPage(
        ApplicationProjectionSettingsService settingsService,
        DisplayProfileService displayProfileService,
        IApplicationWindowController windowController,
        IInstalledApplicationCatalog installedApplicationCatalog,
        GeneralSettingsService generalSettings)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _displayProfileService = displayProfileService ?? throw new ArgumentNullException(nameof(displayProfileService));
        _windowController = windowController ?? throw new ArgumentNullException(nameof(windowController));
        _installedApplicationCatalog = installedApplicationCatalog ?? throw new ArgumentNullException(nameof(installedApplicationCatalog));
        _generalSettings = generalSettings ?? throw new ArgumentNullException(nameof(generalSettings));
        Layouts = new ObservableCollection<ProjectionLayoutViewModel>
        {
            new(ProjectionLayout.KeepSize, "保持尺寸"),
            new(ProjectionLayout.Maximized, "最大化"),
            new(ProjectionLayout.LeftHalf, "左半屏"),
            new(ProjectionLayout.RightHalf, "右半屏"),
        };

        InitializeComponent();
        DataContext = this;
        var toastTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2.2),
            IsEnabled = false,
        };
        toastTimer.Tick += (_, _) =>
        {
            toastTimer.Stop();
            ActionToast.Visibility = Visibility.Collapsed;
        };
        _toastTimer = toastTimer;
        Refresh();
    }

    public ObservableCollection<DisplayTargetViewModel> GlobalTargets { get; } = [];
    public ObservableCollection<DisplayTargetViewModel> RuleTargets { get; } = [];
    public ObservableCollection<ProjectionLayoutViewModel> Layouts { get; }
    public ObservableCollection<ApplicationRuleViewModel> Rules { get; } = [];

    public void Refresh()
    {
        _isApplyingState = true;
        var selectedApplication = (RulesList.SelectedItem as ApplicationRuleViewModel)?.Rule.Application;
        var connected = _displayProfileService.States.Where(state => state.IsConnected).ToArray();
        var connectedIds = connected.Select(state => state.Profile.StableId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _primaryDisplayId = connected.FirstOrDefault(state => state.CurrentDisplay?.IsPrimary == true)?.Profile.StableId;

        GlobalTargets.Clear();
        GlobalTargets.Add(new DisplayTargetViewModel(null, "跟随 Windows 主显示器（默认）", true));
        RuleTargets.Clear();
        for (var index = 0; index < connected.Length; index++)
        {
            var state = connected[index];
            var name = $"屏幕 {index + 1} · {state.DisplayName}";
            var target = new DisplayTargetViewModel(state.Profile.StableId, name, true);
            GlobalTargets.Add(target);
            RuleTargets.Add(target);
        }

        var settings = _settingsService.Current;
        if (settings.DefaultTargetDisplayId is not null && !connectedIds.Contains(settings.DefaultTargetDisplayId))
        {
            GlobalTargets.Add(new DisplayTargetViewModel(settings.DefaultTargetDisplayId, "目标不可用（保留配置）", false));
        }

        DefaultTargetComboBox.SelectedItem = GlobalTargets.FirstOrDefault(target =>
            StringComparer.OrdinalIgnoreCase.Equals(target.StableId, settings.DefaultTargetDisplayId)) ?? GlobalTargets[0];
        ProjectionEnabledToggle.IsEnabled = connected.Length > 1;
        ProjectionEnabledToggle.IsChecked = settings.IsEnabled;
        ProjectionEnabledStateText.Text = settings.IsEnabled ? "已开启" : "已关闭";

        Rules.Clear();
        foreach (var rule in settings.Rules)
        {
            var targetState = connected.FirstOrDefault(state =>
                StringComparer.OrdinalIgnoreCase.Equals(state.Profile.StableId, rule.TargetDisplayId));
            Rules.Add(ApplicationRuleViewModel.Create(rule, targetState?.DisplayName, targetState is not null));
        }

        MissingTargetNotice.Visibility =
            (settings.DefaultTargetDisplayId is not null && !connectedIds.Contains(settings.DefaultTargetDisplayId)) ||
            settings.Rules.Any(rule => !connectedIds.Contains(rule.TargetDisplayId))
                ? Visibility.Visible
                : Visibility.Collapsed;
        EmptyState.Visibility = Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RulesList.SelectedItem = selectedApplication is null
            ? null
            : Rules.FirstOrDefault(item => item.Rule.Application.Matches(selectedApplication));
        _isApplyingState = false;
        ApplySelectedRule();
    }

    public void ShowRuntimeResult(ApplicationProjectionRuntimeResult result)
    {
        Dispatcher.Invoke(() =>
        {
            if (result.Status == ApplicationProjectionRuntimeStatus.Failed)
            {
                ShowToast("无法投放新窗口：可能是权限不足或窗口不再可用。", isError: true);
            }
            else if (result.UsedPrimaryFallback)
            {
                ShowToast("目标显示器暂不可用，已投放到 Windows 主显示器。", isWarning: true);
            }
            else
            {
                ShowToast($"已投放 {result.ApplicationName} 的新窗口。" );
            }
        });
    }

    private void ProjectionEnabledToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isApplyingState || !IsInitialized)
        {
            return;
        }

        SaveGlobalSettings("新窗口自动投放设置已保存。");
    }

    private void DefaultTargetComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingState || !IsInitialized)
        {
            return;
        }

        SaveGlobalSettings("默认打开显示器已保存。");
    }

    private void SaveGlobalSettings(string message)
    {
        try
        {
            var target = DefaultTargetComboBox.SelectedItem as DisplayTargetViewModel;
            _settingsService.UpdateGlobalSettings(ProjectionEnabledToggle.IsChecked == true, target?.StableId);
            ProjectionEnabledStateText.Text = _settingsService.Current.IsEnabled ? "已开启" : "已关闭";
            ShowToast(message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Refresh();
            ShowToast($"设置保存失败：{exception.Message}", isError: true);
        }
    }

    private void RulesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => ApplySelectedRule();

    private void ApplySelectedRule()
    {
        _isApplyingState = true;
        if (RulesList.SelectedItem is not ApplicationRuleViewModel selected)
        {
            SelectedRuleNameText.Text = "请选择一条规则";
            RuleTargetComboBox.SelectedItem = null;
            LayoutComboBox.SelectedItem = null;
            RuleEnabledToggle.IsChecked = false;
            RuleEnabledStateText.Text = "未选择";
            RuleTargetComboBox.IsEnabled = false;
            LayoutComboBox.IsEnabled = false;
            RuleEnabledToggle.IsEnabled = false;
            SaveRuleButton.IsEnabled = false;
            DeleteRuleButton.IsEnabled = false;
            RuleWarning.Visibility = Visibility.Collapsed;
        }
        else
        {
            SelectedRuleNameText.Text = selected.ApplicationName;
            var target = RuleTargets.FirstOrDefault(item =>
                StringComparer.OrdinalIgnoreCase.Equals(item.StableId, selected.Rule.TargetDisplayId));
            RuleTargetComboBox.SelectedItem = target;
            LayoutComboBox.SelectedItem = Layouts.First(item => item.Layout == selected.Rule.Layout);
            RuleEnabledToggle.IsChecked = selected.Rule.IsEnabled;
            RuleEnabledStateText.Text = selected.Rule.IsEnabled ? "已启用" : "已暂停";
            RuleTargetComboBox.IsEnabled = RuleTargets.Count > 0;
            LayoutComboBox.IsEnabled = true;
            RuleEnabledToggle.IsEnabled = true;
            SaveRuleButton.IsEnabled = RuleTargets.Count > 0;
            DeleteRuleButton.IsEnabled = true;
            RuleWarning.Visibility = selected.IsTargetAvailable ? Visibility.Collapsed : Visibility.Visible;
        }

        _isApplyingState = false;
    }

    private void RuleEnabledToggle_OnChanged(object sender, RoutedEventArgs e)
    {
        if (!_isApplyingState)
        {
            RuleEnabledStateText.Text = RuleEnabledToggle.IsChecked == true ? "已启用" : "已暂停";
        }
    }

    private void RunningAppButton_OnClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<ApplicationWindowSnapshot> applications;
        try
        {
            applications = _windowController.ReadAll();
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            ShowToast($"无法读取运行中的应用：{exception.Message}", isError: true);
            return;
        }

        if (applications.Count == 0)
        {
            ShowToast("没有找到可稳定识别的普通应用窗口。", isWarning: true);
            return;
        }

        var dialog = new RunningApplicationDialog(applications) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.SelectedApplication is not null)
        {
            AddOrSelectApplication(dialog.SelectedApplication.Application, dialog.SelectedApplication.DisplayName);
        }
    }

    private void InstalledAppButton_OnClick(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<InstalledApplication> applications;
        try
        {
            applications = _installedApplicationCatalog.ReadAll();
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException)
        {
            ShowToast($"无法读取 Windows 应用列表：{exception.Message}", isError: true);
            return;
        }

        if (applications.Count == 0)
        {
            ShowToast("Windows 应用列表中没有找到可添加的应用。", isWarning: true);
            return;
        }

        var dialog = new InstalledApplicationDialog(applications) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.SelectedApplication is not null)
        {
            AddOrSelectApplication(
                dialog.SelectedApplication.Identity,
                dialog.SelectedApplication.DisplayName);
        }
    }

    private void AddOrSelectApplication(ProjectionApplicationIdentity identity, string displayName)
    {
        var existing = Rules.FirstOrDefault(item => item.Rule.Application.Matches(identity));
        if (existing is not null)
        {
            RulesList.SelectedItem = existing;
            RulesList.ScrollIntoView(existing);
            ShowToast("该应用已有规则，已定位到现有配置。", isWarning: true);
            return;
        }

        var target = RuleTargets.FirstOrDefault(item =>
            StringComparer.OrdinalIgnoreCase.Equals(item.StableId, _primaryDisplayId)) ?? RuleTargets.FirstOrDefault();
        if (target?.StableId is null)
        {
            ShowToast("当前没有可用的目标显示器。", isWarning: true);
            return;
        }

        try
        {
            var rule = new ApplicationProjectionRule(
                identity,
                displayName,
                target.StableId,
                ProjectionLayout.KeepSize,
                true);
            _settingsService.SaveRule(rule);
            Refresh();
            RulesList.SelectedItem = Rules.First(item => item.Rule.Application.Matches(identity));
            ShowToast("应用规则已添加，可继续调整并保存。" );
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowToast($"规则保存失败：{exception.Message}", isError: true);
        }
    }

    private void SaveRuleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not ApplicationRuleViewModel selected ||
            RuleTargetComboBox.SelectedItem is not DisplayTargetViewModel target ||
            target.StableId is null ||
            LayoutComboBox.SelectedItem is not ProjectionLayoutViewModel layout)
        {
            ShowToast("请选择可用的目标显示器和打开布局。", isWarning: true);
            return;
        }

        try
        {
            _settingsService.SaveRule(new ApplicationProjectionRule(
                selected.Rule.Application,
                selected.Rule.DisplayName,
                target.StableId,
                layout.Layout,
                RuleEnabledToggle.IsChecked == true));
            var identity = selected.Rule.Application;
            Refresh();
            RulesList.SelectedItem = Rules.First(item => item.Rule.Application.Matches(identity));
            ShowToast("应用规则已保存。" );
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowToast($"规则保存失败：{exception.Message}", isError: true);
        }
    }

    private void DeleteRuleButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not ApplicationRuleViewModel selected)
        {
            return;
        }

        var confirmed = MessageBox.Show(
            Window.GetWindow(this),
            $"确定删除“{selected.ApplicationName}”的应用投放规则吗？",
            "删除应用规则",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (confirmed != MessageBoxResult.OK)
        {
            return;
        }

        try
        {
            _settingsService.DeleteRule(selected.Rule.Application);
            Refresh();
            ShowToast("应用规则已删除。" );
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowToast($"规则删除失败：{exception.Message}", isError: true);
        }
    }

    private void ShowToast(string message, bool isError = false, bool isWarning = false)
    {
        var severity = isError
            ? UserNotificationSeverity.Error
            : isWarning
                ? UserNotificationSeverity.Warning
                : UserNotificationSeverity.Success;
        if (!UserNotificationPolicy.ShouldShow(_generalSettings.Current.ShowSuccessNotifications, severity))
        {
            return;
        }

        ToastText.Text = message;
        ToastIndicator.Fill = (Brush)FindResource(isError ? "ErrorBrush" : isWarning ? "WarningBrush" : "SuccessBrush");
        ActionToast.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }
}
