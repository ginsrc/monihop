using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using MoniHop.Desktop.Diagnostics;
using MoniHop.Desktop.Localization;
using MoniHop.Desktop.Notifications;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.Updates;
using MoniHop.Windows.Security;

namespace MoniHop.Desktop.Views;

public partial class AboutPage : UserControl
{
    private readonly GeneralSettingsService _settings;
    private readonly GitHubUpdateCheckService _updates;
    private readonly UpdateInstallerService _updateInstaller;
    private readonly LocalDiagnosticService _diagnostics;
    private readonly DisplayProfileService _displayProfiles;
    private readonly IProcessElevationService _elevation;
    private readonly LocalizationService _localization;
    private readonly DispatcherTimer _toastTimer;
    private readonly bool _isPortable;
    private UpdateCheckResult? _lastUpdateResult;
    private Uri? _latestReleaseUri;
    private bool _checkingForUpdates;
    private bool _installingUpdate;
    private bool _refreshing;

    public AboutPage(
        MoniHopPaths paths,
        GeneralSettingsService settings,
        GitHubUpdateCheckService updates,
        UpdateInstallerService updateInstaller,
        LocalDiagnosticService diagnostics,
        DisplayProfileService displayProfiles,
        IProcessElevationService elevation,
        LocalizationService localization)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        _updateInstaller = updateInstaller ?? throw new ArgumentNullException(nameof(updateInstaller));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _displayProfiles = displayProfiles ?? throw new ArgumentNullException(nameof(displayProfiles));
        _elevation = elevation ?? throw new ArgumentNullException(nameof(elevation));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _isPortable = paths.IsPortable;

        Version = ProductInfo.Version;
        OperatingSystem = RuntimeInformation.OSDescription;
        Runtime = RuntimeInformation.FrameworkDescription;
        ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
        ExecutableDirectory = AppContext.BaseDirectory;
        DataDirectory = paths.DataDirectory;
        ConfigurationDirectory = paths.ConfigurationDirectory;
        DiagnosticsDirectory = paths.DiagnosticsDirectory;
        LicensePath = Path.Combine(AppContext.BaseDirectory, "LICENSE");

        InitializeComponent();
        DataContext = this;
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _toastTimer.Tick += ToastTimer_OnTick;
        Refresh();
    }

    public string Version { get; }

    public string OperatingSystem { get; }

    public string Runtime { get; }

    public string ProcessArchitecture { get; }

    public string ExecutableDirectory { get; }

    public string DataDirectory { get; }

    public string ConfigurationDirectory { get; }

    public string DiagnosticsDirectory { get; }

    public string LicensePath { get; }

    public void Refresh()
    {
        _refreshing = true;
        AutomaticUpdateCheckToggle.IsChecked = _settings.Current.CheckForUpdatesAutomatically;
        DetailedDiagnosticsToggle.IsChecked = _settings.Current.DetailedDiagnosticsEnabled;
        RunIdentityText.Text = _localization.Get(
            _elevation.IsElevated
                ? "String.About.Diagnostics.Admin"
                : "String.About.Diagnostics.User");
        ConnectedDisplayCountText.Text = string.Format(
            _localization.Get("String.About.Diagnostics.DisplayCount"),
            _displayProfiles.States.Count(state => state.IsConnected));
        DiagnosticLogSizeText.Text = FormatByteSize(_diagnostics.LogSize);
        RenderUpdateStatus();
        _refreshing = false;
    }

    public Task CheckForUpdatesOnStartupAsync() =>
        _settings.Current.CheckForUpdatesAutomatically
            ? CheckForUpdatesAsync(userInitiated: false)
            : Task.CompletedTask;

    private void ProjectButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenUri(new Uri(ProductInfo.RepositoryUrl));

    private void ReleasesButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenUri(new Uri(ProductInfo.ReleasesUrl));

    private void IssuesButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenUri(new Uri(ProductInfo.IssuesUrl));

    private void LicenseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(LicensePath))
        {
            ShowToast(_localization.Get("String.About.Product.LicenseMissing"), isError: true);
            return;
        }

        OpenPath(LicensePath, createDirectory: false);
    }

    private async void CheckUpdatesButton_OnClick(object sender, RoutedEventArgs e) =>
        await CheckForUpdatesAsync(userInitiated: true);

    private async void AutomaticUpdateCheckToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (_refreshing)
        {
            return;
        }

        var enabled = AutomaticUpdateCheckToggle.IsChecked == true;
        if (!_settings.Update(_settings.Current with { CheckForUpdatesAutomatically = enabled }))
        {
            ShowSettingsError();
            Refresh();
            return;
        }

        ShowToast(_localization.Get("String.About.Status.Saved"));
        if (enabled)
        {
            await CheckForUpdatesAsync(userInitiated: false);
        }
    }

    private async Task CheckForUpdatesAsync(bool userInitiated)
    {
        if (_checkingForUpdates)
        {
            return;
        }

        _checkingForUpdates = true;
        CheckUpdatesButton.IsEnabled = false;
        RenderUpdateStatus();
        try
        {
            _lastUpdateResult = await _updates.CheckAsync();
            _latestReleaseUri = _lastUpdateResult.ReleaseUri;
            if (userInitiated && _lastUpdateResult.Status == UpdateCheckStatus.Failed)
            {
                ShowToast(_localization.Get("String.About.Update.Failed"), isError: true);
            }
        }
        finally
        {
            _checkingForUpdates = false;
            CheckUpdatesButton.IsEnabled = true;
            RenderUpdateStatus();
        }
    }

    private void RenderUpdateStatus()
    {
        OpenUpdateButton.Visibility = Visibility.Collapsed;
        OpenUpdateButton.IsEnabled = true;
        CheckUpdatesButton.IsEnabled = !_installingUpdate;
        if (_installingUpdate)
        {
            UpdateStatusText.Text = _localization.Get("String.About.Update.Installing");
            return;
        }

        if (_checkingForUpdates)
        {
            UpdateStatusText.Text = _localization.Get("String.About.Update.Checking");
            return;
        }

        if (_lastUpdateResult is null)
        {
            UpdateStatusText.Text = _localization.Get("String.About.Update.NotChecked");
            return;
        }

        UpdateStatusText.Text = _lastUpdateResult.Status switch
        {
            UpdateCheckStatus.UpdateAvailable => string.Format(
                _localization.Get("String.About.Update.Available"),
                _lastUpdateResult.LatestVersion),
            UpdateCheckStatus.UpToDate => string.Format(
                _localization.Get("String.About.Update.UpToDate"),
                _lastUpdateResult.LatestVersion),
            UpdateCheckStatus.NoRelease => _localization.Get("String.About.Update.NoRelease"),
            _ => _localization.Get("String.About.Update.Failed"),
        };

        if (_latestReleaseUri is not null &&
            _lastUpdateResult.Status == UpdateCheckStatus.UpdateAvailable)
        {
            OpenUpdateButton.Content = _localization.Get(
                CanInstallUpdate
                    ? "String.About.Update.DownloadInstall"
                    : "String.About.Update.OpenRelease");
            OpenUpdateButton.Visibility = Visibility.Visible;
        }
    }

    private async void OpenUpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CanInstallUpdate)
        {
            await InstallUpdateAsync(_lastUpdateResult!.InstallerPackage!);
            return;
        }

        if (_latestReleaseUri is not null)
        {
            OpenUri(_latestReleaseUri);
        }
    }

    private bool CanInstallUpdate =>
        !_isPortable &&
        _lastUpdateResult?.Status == UpdateCheckStatus.UpdateAvailable &&
        _lastUpdateResult.InstallerPackage is not null;

    private async Task InstallUpdateAsync(UpdateInstallerPackage package)
    {
        if (_installingUpdate)
        {
            return;
        }

        _installingUpdate = true;
        OpenUpdateButton.IsEnabled = false;
        RenderUpdateStatus();
        UpdateInstallResult result;
        try
        {
            result = await _updateInstaller.DownloadVerifyAndLaunchAsync(package);
        }
        finally
        {
            _installingUpdate = false;
        }

        if (result.Status == UpdateInstallStatus.Started)
        {
            Application.Current.Shutdown();
            return;
        }

        RenderUpdateStatus();
        var messageKey = result.Failure switch
        {
            UpdateInstallFailure.ChecksumMissing or UpdateInstallFailure.ChecksumMismatch =>
                "String.About.Update.ChecksumFailed",
            UpdateInstallFailure.LaunchFailed => "String.About.Update.LaunchFailed",
            _ => "String.About.Update.InstallFailed",
        };
        ShowToast(_localization.Get(messageKey), isError: true);
    }

    private void DetailedDiagnosticsToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (_refreshing)
        {
            return;
        }

        var enabled = DetailedDiagnosticsToggle.IsChecked == true;
        if (!_settings.Update(_settings.Current with { DetailedDiagnosticsEnabled = enabled }))
        {
            ShowSettingsError();
            Refresh();
            return;
        }

        if (enabled)
        {
            _diagnostics.Write("diagnostics.enabled", "Detailed local diagnostics enabled.");
        }

        Refresh();
        ShowToast(_localization.Get("String.About.Status.Saved"));
    }

    private void ExportDiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".json",
            FileName = $"MoniHop-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.json",
            Filter = _localization.Get("String.About.Diagnostics.ExportFilter"),
            Title = _localization.Get("String.About.Diagnostics.ExportTitle"),
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        try
        {
            _diagnostics.Export(dialog.FileName, CreateDiagnosticSnapshot());
            ShowToast(_localization.Get("String.About.Diagnostics.Exported"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowToast(string.Format(
                _localization.Get("String.About.Diagnostics.ExportFailed"),
                exception.Message), isError: true);
        }
    }

    private void ClearDiagnosticsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            Window.GetWindow(this),
            _localization.Get("String.About.Diagnostics.ClearPrompt"),
            _localization.Get("String.About.Diagnostics.ClearTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _diagnostics.Clear();
            Refresh();
            ShowToast(_localization.Get("String.About.Diagnostics.Cleared"));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowToast(string.Format(
                _localization.Get("String.About.Diagnostics.ClearFailed"),
                exception.Message), isError: true);
        }
    }

    private void OpenDiagnosticsDirectoryButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenPath(DiagnosticsDirectory, createDirectory: true);

    private void CopyPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path } || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            Clipboard.SetText(path);
            ShowToast(_localization.Get("String.About.Paths.Copied"));
        }
        catch (ExternalException exception)
        {
            ShowToast(string.Format(
                _localization.Get("String.About.Paths.CopyFailed"),
                exception.Message), isError: true);
        }
    }

    private void OpenPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string path } && !string.IsNullOrWhiteSpace(path))
        {
            OpenPath(path, createDirectory: true);
        }
    }

    private void OpenUri(Uri uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            ShowToast(string.Format(
                _localization.Get("String.About.Status.OpenFailed"),
                exception.Message), isError: true);
        }
    }

    private void OpenPath(string path, bool createDirectory)
    {
        try
        {
            if (createDirectory)
            {
                Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (
            exception is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowToast(string.Format(
                _localization.Get("String.About.Status.OpenFailed"),
                exception.Message), isError: true);
        }
    }

    private DiagnosticSnapshot CreateDiagnosticSnapshot() => new(
        ProductInfo.Version,
        OperatingSystem,
        ProcessArchitecture,
        _elevation.IsElevated ? "Administrator" : "Standard user",
        _displayProfiles.States.Count(state => state.IsConnected),
        ConfigurationLoaded: JsonStoreRecovery.RecoveryCount == 0);

    private void ShowSettingsError() => ShowToast(
        string.Format(
            _localization.Get("String.About.Status.SaveFailed"),
            string.IsNullOrWhiteSpace(_settings.LastError)
                ? _localization.Get("String.General.Status.UnknownError")
                : _settings.LastError),
        isError: true);

    private void ShowToast(string message, bool isError = false)
    {
        if (!UserNotificationPolicy.ShouldShow(
                _settings.Current.ShowSuccessNotifications,
                isError ? UserNotificationSeverity.Error : UserNotificationSeverity.Success))
        {
            return;
        }

        ToastText.Text = message;
        ToastIndicator.Fill = Application.Current?.TryFindResource(
            isError ? "ErrorBrush" : "SuccessBrush") as Brush
            ?? (isError ? Brushes.IndianRed : Brushes.SeaGreen);
        ActionToast.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ToastTimer_OnTick(object? sender, EventArgs e)
    {
        _toastTimer.Stop();
        ActionToast.Visibility = Visibility.Collapsed;
    }

    private static string FormatByteSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.#} KB";
        }

        return $"{bytes / (1024d * 1024d):0.#} MB";
    }
}
