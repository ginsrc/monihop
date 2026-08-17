using System.Windows;
using Microsoft.Win32;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Theming;

public sealed class ThemeService
{
    private const string DictionaryMarker = "Themes/";
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public AppTheme Preference { get; private set; } = AppTheme.System;

    public AppTheme AppliedTheme { get; private set; } = AppTheme.Light;

    public void Apply(AppTheme preference)
    {
        Preference = preference;
        var theme = ResolveTheme(preference, WindowsUsesLightTheme());
        var application = Application.Current;
        if (application is not null)
        {
            var dictionaries = application.Resources.MergedDictionaries;
            var index = dictionaries
                .Select((dictionary, position) => (dictionary, position))
                .FirstOrDefault(item => item.dictionary.Source?.OriginalString.Contains(
                    DictionaryMarker,
                    StringComparison.OrdinalIgnoreCase) == true &&
                    item.dictionary.Source.OriginalString.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) &&
                    !item.dictionary.Source.OriginalString.EndsWith("Controls.xaml", StringComparison.OrdinalIgnoreCase))
                .position;
            var replacement = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}.xaml", UriKind.Relative),
            };
            if (index >= 0 && index < dictionaries.Count)
            {
                dictionaries[index] = replacement;
            }
            else
            {
                dictionaries.Insert(0, replacement);
            }
        }

        AppliedTheme = theme;
    }

    public void RefreshSystemTheme()
    {
        if (Preference == AppTheme.System)
        {
            Apply(AppTheme.System);
        }
    }

    public static AppTheme ResolveTheme(AppTheme preference, bool windowsUsesLightTheme) =>
        preference == AppTheme.System
            ? windowsUsesLightTheme ? AppTheme.Light : AppTheme.Dark
            : preference;

    private static bool WindowsUsesLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
    }
}
