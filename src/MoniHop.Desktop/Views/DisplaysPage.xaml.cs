using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MoniHop.Core.Displays;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Views;

public partial class DisplaysPage : UserControl
{
    private readonly DisplayProfileService _profileService;

    public DisplaysPage(DisplayProfileService profileService)
    {
        InitializeComponent();
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        ViewModel = new DisplayHistoryViewModel();
        DataContext = this;
        Refresh();
    }

    public DisplayHistoryViewModel ViewModel { get; }

    public ObservableCollection<DisplayViewModel> ConnectedDisplays => ViewModel.ConnectedDisplays;

    public ObservableCollection<DisplayViewModel> AllDisplays => ViewModel.AllDisplays;

    public void Refresh()
    {
        ViewModel.Refresh(_profileService.States);
        RefreshStatusText.Text = $"已实时更新 · 当前连接 {ConnectedDisplays.Count} 台显示器";
    }

    public void ShowRefreshFailure(string message)
    {
        RefreshStatusText.Text = $"显示器状态更新失败：{message}";
        RefreshStatusText.Foreground = (System.Windows.Media.Brush)FindResource("ErrorBrush");
    }

    private void Rename_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not DisplayViewModel display)
        {
            return;
        }

        var dialog = new DisplayRenameDialog(display.Name)
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _profileService.Rename(display.StableId, dialog.DisplayName);
            Refresh();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowRefreshFailure($"名称保存失败：{exception.Message}");
        }
    }

    private void Forget_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not DisplayViewModel display || display.IsConnected)
        {
            return;
        }

        var confirmed = MessageBox.Show(
            Window.GetWindow(this),
            $"确定忘记“{display.Name}”吗？这只会删除 MoniHop 本地记录。",
            "忘记显示器",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (confirmed != MessageBoxResult.OK)
        {
            return;
        }

        try
        {
            _profileService.Forget(display.StableId);
            Refresh();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowRefreshFailure($"删除记录失败：{exception.Message}");
        }
    }
}
