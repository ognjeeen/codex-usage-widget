using System.Globalization;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Localization;

public sealed class AppLanguageController
{
    private readonly LanguagePreferenceStore? _store;
    private readonly CultureInfo _systemUiCulture;

    public AppLanguageController(
        LanguagePreferenceStore store,
        CultureInfo? systemUiCulture = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _systemUiCulture = systemUiCulture ?? CultureInfo.CurrentUICulture;
        Preference = store.Load();
        ApplyPreference();
    }

    public AppLanguageController(
        LanguagePreference preference,
        CultureInfo? systemUiCulture = null)
    {
        _systemUiCulture = systemUiCulture ?? CultureInfo.CurrentUICulture;
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
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo(cultureName));
    }
}
