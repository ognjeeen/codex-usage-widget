using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class ThemePreferenceResolverTests
{
    [Theory]
    [InlineData(ThemePreference.System, true, EffectiveTheme.Light)]
    [InlineData(ThemePreference.System, false, EffectiveTheme.Dark)]
    [InlineData(ThemePreference.Light, false, EffectiveTheme.Light)]
    [InlineData(ThemePreference.Dark, true, EffectiveTheme.Dark)]
    public void ResolveUsesPreferenceOrSystemTheme(
        ThemePreference preference,
        bool systemUsesLightTheme,
        EffectiveTheme expected)
    {
        Assert.Equal(
            expected,
            ThemePreferenceResolver.Resolve(preference, systemUsesLightTheme));
    }
}
