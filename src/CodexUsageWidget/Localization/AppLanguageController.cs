using System.Globalization;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Localization;

public sealed class AppLanguageController
{
    private readonly LanguagePreferenceStore? _store;
    private readonly CultureInfo _systemUiCulture;
    private readonly CultureInfo _windowsRegionalCulture;

    public AppLanguageController(
        LanguagePreferenceStore store,
        CultureInfo? systemUiCulture = null,
        CultureInfo? windowsRegionalCulture = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _systemUiCulture = systemUiCulture ?? CultureInfo.CurrentUICulture;
        _windowsRegionalCulture = windowsRegionalCulture ?? CultureInfo.CurrentCulture;
        Preference = store.Load();
        ApplyPreference();
    }

    public AppLanguageController(
        LanguagePreference preference,
        CultureInfo? systemUiCulture = null,
        CultureInfo? windowsRegionalCulture = null)
    {
        _systemUiCulture = systemUiCulture ?? CultureInfo.CurrentUICulture;
        _windowsRegionalCulture = windowsRegionalCulture ?? CultureInfo.CurrentCulture;
        Preference = preference;
        ApplyPreference();
    }

    public LanguagePreference Preference { get; private set; }

    public LanguagePreference EffectiveLanguage =>
        LanguagePreferenceResolver.Resolve(Preference, _systemUiCulture);

    public void SetPreference(LanguagePreference preference)
    {
        Preference = preference;
        _store?.Save(preference);
        ApplyPreference();
    }

    private void ApplyPreference()
    {
        var cultureName = EffectiveLanguage == LanguagePreference.SimplifiedChinese
            ? "zh-CN"
            : "en-US";
        Strings.Current.SetCulture(
            CultureInfo.GetCultureInfo(cultureName),
            _windowsRegionalCulture);
    }
}
