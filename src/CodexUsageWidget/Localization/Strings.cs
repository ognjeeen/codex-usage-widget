using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace CodexUsageWidget.Localization;

public sealed class Strings : INotifyPropertyChanged
{
    private static readonly ResourceManager ResourceManager = new(
        "CodexUsageWidget.Resources.Strings",
        typeof(Strings).Assembly);
    private CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");
    private CultureInfo _windowsRegionalCulture = CultureInfo.CurrentCulture;

    private Strings()
    {
    }

    public static Strings Current { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Get(key);

    public CultureInfo Culture => _culture;

    public CultureInfo WindowsRegionalCulture => _windowsRegionalCulture;

    public static string Get(string key) =>
        ResourceManager.GetString(key, Current._culture) ?? key;

    public static string GetForCulture(string key, CultureInfo culture) =>
        ResourceManager.GetString(key, culture) ?? key;

    public static string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    public static string FormatForCulture(
        CultureInfo culture,
        string key,
        params object?[] arguments) =>
        string.Format(culture, GetForCulture(key, culture), arguments);

    public void SetCulture(
        CultureInfo culture,
        CultureInfo? windowsRegionalCulture = null)
    {
        ArgumentNullException.ThrowIfNull(culture);
        if (windowsRegionalCulture is not null)
        {
            _windowsRegionalCulture = windowsRegionalCulture;
        }

        var changed = !string.Equals(
            _culture.Name,
            culture.Name,
            StringComparison.OrdinalIgnoreCase);
        _culture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        if (changed)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }
}
