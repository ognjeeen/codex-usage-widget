using System.Globalization;

namespace CodexUsageWidget.Infrastructure.Settings;

public static class LanguagePreferenceResolver
{
    public static LanguagePreference Resolve(
        LanguagePreference preference,
        CultureInfo uiCulture) => preference switch
        {
            LanguagePreference.English => LanguagePreference.English,
            LanguagePreference.SimplifiedChinese => LanguagePreference.SimplifiedChinese,
            LanguagePreference.Russian => LanguagePreference.Russian,
            _ when uiCulture.TwoLetterISOLanguageName == "ru" => LanguagePreference.Russian,
            _ when IsSimplifiedChinese(uiCulture) => LanguagePreference.SimplifiedChinese,
            _ => LanguagePreference.English
        };

    private static bool IsSimplifiedChinese(CultureInfo culture)
    {
        for (var current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
        {
            if (string.Equals(current.Name, "zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
