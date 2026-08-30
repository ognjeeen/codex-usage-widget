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
            _ => IsSimplifiedChinese(uiCulture)
                ? LanguagePreference.SimplifiedChinese
                : LanguagePreference.English
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
