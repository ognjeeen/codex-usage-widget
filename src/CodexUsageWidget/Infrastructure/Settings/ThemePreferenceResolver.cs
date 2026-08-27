namespace CodexUsageWidget.Infrastructure.Settings;

public static class ThemePreferenceResolver
{
    public static EffectiveTheme Resolve(
        ThemePreference preference,
        bool systemUsesLightTheme) => preference switch
        {
            ThemePreference.Light => EffectiveTheme.Light,
            ThemePreference.Dark => EffectiveTheme.Dark,
            _ => systemUsesLightTheme ? EffectiveTheme.Light : EffectiveTheme.Dark
        };
}
