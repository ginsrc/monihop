using System.IO;
using System.Windows;
using System.Windows.Controls;
using MoniHop.Core.Cursors;
using MoniHop.Desktop.Localization;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.Theming;
using MoniHop.Windows.Security;
using MoniHop.Windows.Startup;

namespace MoniHop.Desktop.Views;

public partial class BehaviorPage : UserControl
{
    private readonly GeneralSettingsService _settings;
    private readonly IStartupRegistrationService _startup;
    private readonly IProcessElevationService _elevation;
    private readonly ThemeService _theme;
    private readonly LocalizationService _localization;
    private bool _refreshing;

    public BehaviorPage(
        GeneralSettingsService settings,
        IStartupRegistrationService startup,
        IProcessElevationService elevation,
        ThemeService theme,
        LocalizationService localization)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _startup = startup ?? throw new ArgumentNullException(nameof(startup));
        _elevation = elevation ?? throw new ArgumentNullException(nameof(elevation));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        InitializeComponent();
        Refresh();
    }

    public event EventHandler? RestartElevatedRequested;

    public void ShowElevationResult(ElevationRestartResult result)
    {
        StatusText.Text = _localization.Get(result switch
        {
            ElevationRestartResult.Canceled => "String.General.Permission.Canceled",
            _ => "String.General.Permission.Failed",
        });
    }

    public void Refresh()
    {
        _refreshing = true;
        var current = _settings.Current;
        StartWithWindowsToggle.IsChecked = current.StartWithWindows;
        CloseBehaviorComboBox.SelectedIndex = (int)current.CloseBehavior;
        CursorLandingComboBox.SelectedIndex = (int)current.CursorLanding;
        RecallOffscreenToggle.IsChecked = current.RecallOffscreenWindows;
        SuccessNotificationsToggle.IsChecked = current.ShowSuccessNotifications;
        AlwaysAdminToggle.IsChecked = current.AlwaysRunAsAdministrator;
        AlwaysAdminToggle.IsEnabled = _elevation.IsElevated;
        RestartElevatedButton.Visibility = _elevation.IsElevated ? Visibility.Collapsed : Visibility.Visible;
        PermissionStatusText.Text = _localization.Get(
            _elevation.IsElevated
                ? "String.General.Permission.Admin"
                : "String.General.Permission.User");
        ThemeComboBox.SelectedIndex = (int)current.Theme;
        LanguageComboBox.SelectedIndex = (int)current.Language;
        _refreshing = false;
    }

    private void StartWithWindowsToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (_refreshing)
        {
            return;
        }

        var enabled = StartWithWindowsToggle.IsChecked == true;
        var previous = _settings.Current;
        try
        {
            _startup.Apply(enabled, previous.AlwaysRunAsAdministrator);
            if (!_settings.Update(previous with { StartWithWindows = enabled }))
            {
                _startup.Apply(previous.StartWithWindows, previous.AlwaysRunAsAdministrator);
                ShowError(_settings.LastError);
            }
            else
            {
                ShowSaved();
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowError(exception.Message);
        }

        Refresh();
    }

    private void CloseBehaviorComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_refreshing && CloseBehaviorComboBox.SelectedIndex >= 0)
        {
            Save(_settings.Current with { CloseBehavior = (CloseBehavior)CloseBehaviorComboBox.SelectedIndex });
        }
    }

    private void CursorLandingComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_refreshing && CursorLandingComboBox.SelectedIndex >= 0)
        {
            Save(_settings.Current with { CursorLanding = (CursorLandingMode)CursorLandingComboBox.SelectedIndex });
        }
    }

    private void RecallOffscreenToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_refreshing)
        {
            Save(_settings.Current with { RecallOffscreenWindows = RecallOffscreenToggle.IsChecked == true });
        }
    }

    private void SuccessNotificationsToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_refreshing)
        {
            Save(_settings.Current with { ShowSuccessNotifications = SuccessNotificationsToggle.IsChecked == true });
        }
    }

    private void RestartElevatedButton_OnClick(object sender, RoutedEventArgs e) =>
        RestartElevatedRequested?.Invoke(this, EventArgs.Empty);

    private void AlwaysAdminToggle_OnClick(object sender, RoutedEventArgs e)
    {
        if (_refreshing || !_elevation.IsElevated)
        {
            Refresh();
            return;
        }

        var previous = _settings.Current;
        var enabled = AlwaysAdminToggle.IsChecked == true;
        try
        {
            if (previous.StartWithWindows)
            {
                _startup.Apply(enabled: true, runElevated: enabled);
            }

            if (!_settings.Update(previous with { AlwaysRunAsAdministrator = enabled }))
            {
                if (previous.StartWithWindows)
                {
                    _startup.Apply(enabled: true, runElevated: previous.AlwaysRunAsAdministrator);
                }
                ShowError(_settings.LastError);
            }
            else
            {
                ShowSaved();
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowError(exception.Message);
        }

        Refresh();
    }

    private void ThemeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || ThemeComboBox.SelectedIndex < 0)
        {
            return;
        }

        var value = (AppTheme)ThemeComboBox.SelectedIndex;
        if (_settings.Update(_settings.Current with { Theme = value }))
        {
            _theme.Apply(value);
            ShowSaved();
        }
        else
        {
            ShowError(_settings.LastError);
            Refresh();
        }
    }

    private void LanguageComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || LanguageComboBox.SelectedIndex < 0)
        {
            return;
        }

        var value = (AppLanguage)LanguageComboBox.SelectedIndex;
        if (_settings.Update(_settings.Current with { Language = value }))
        {
            _localization.Apply(value);
            ShowSaved();
        }
        else
        {
            ShowError(_settings.LastError);
            Refresh();
        }
    }

    private void Save(GeneralSettings value)
    {
        if (_settings.Update(value))
        {
            ShowSaved();
        }
        else
        {
            ShowError(_settings.LastError);
        }
        Refresh();
    }

    private void ShowSaved() =>
        StatusText.Text = _localization.Get("String.General.Status.Saved");

    private void ShowError(string? details) =>
        StatusText.Text = string.Format(
            _localization.Get("String.General.Status.Failed"),
            string.IsNullOrWhiteSpace(details) ? _localization.Get("String.General.Status.UnknownError") : details);
}
