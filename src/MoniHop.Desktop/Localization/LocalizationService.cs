using System.Globalization;
using System.Windows;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Localization;

public sealed class LocalizationService
{
    private const string DictionaryMarker = "Localization/Strings.";

    public event EventHandler? Changed;

    public AppLanguage AppliedLanguage { get; private set; } = AppLanguage.SimplifiedChinese;

    public void Apply(AppLanguage preference)
    {
        var language = ResolveLanguage(preference, CultureInfo.CurrentUICulture);
        var cultureName = language == AppLanguage.SimplifiedChinese ? "zh-CN" : "en-US";
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var application = Application.Current;
        if (application is not null)
        {
            ReplaceDictionary(
                application.Resources.MergedDictionaries,
                DictionaryMarker,
                new Uri($"Localization/Strings.{cultureName}.xaml", UriKind.Relative));
        }

        if (AppliedLanguage != language)
        {
            AppliedLanguage = language;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (Application.Current?.TryFindResource(key) is string value)
        {
            return value;
        }

        var fallback = new ResourceDictionary
        {
            Source = new Uri("Localization/Strings.zh-CN.xaml", UriKind.Relative),
        };
        return fallback[key] as string ?? key;
    }

    public static AppLanguage ResolveLanguage(AppLanguage preference, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (preference != AppLanguage.System)
        {
            return preference;
        }

        if (string.IsNullOrWhiteSpace(culture.Name))
        {
            return AppLanguage.SimplifiedChinese;
        }

        return string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.SimplifiedChinese
            : AppLanguage.English;
    }

    private static void ReplaceDictionary(
        IList<ResourceDictionary> dictionaries,
        string marker,
        Uri source)
    {
        var index = dictionaries
            .Select((dictionary, position) => (dictionary, position))
            .FirstOrDefault(item => item.dictionary.Source?.OriginalString.Contains(
                marker,
                StringComparison.OrdinalIgnoreCase) == true)
            .position;
        var replacement = new ResourceDictionary { Source = source };
        if (index >= 0 && index < dictionaries.Count)
        {
            dictionaries[index] = replacement;
        }
        else
        {
            dictionaries.Insert(Math.Min(1, dictionaries.Count), replacement);
        }
    }
}
