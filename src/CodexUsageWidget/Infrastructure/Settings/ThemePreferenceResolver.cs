namespace CodexUsageWidget.Infrastructure.Settings;

public static class ThemePreferenceResolver
{
    public static EffectiveTheme Resolve(
        ThemePreference preference,
        bool appsUseLightTheme) => preference switch
        {
            ThemePreference.Light => EffectiveTheme.Light,
            ThemePreference.Dark => EffectiveTheme.Dark,
            _ => appsUseLightTheme ? EffectiveTheme.Light : EffectiveTheme.Dark
        };
}
