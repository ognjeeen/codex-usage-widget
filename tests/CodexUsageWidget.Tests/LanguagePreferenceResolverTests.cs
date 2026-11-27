using System.Globalization;
using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class LanguagePreferenceResolverTests
{
    [Theory]
    [InlineData("en-US", LanguagePreference.English)]
    [InlineData("fr-FR", LanguagePreference.English)]
    [InlineData("ru-RU", LanguagePreference.Russian)]
    [InlineData("ru", LanguagePreference.Russian)]
    [InlineData("zh-TW", LanguagePreference.English)]
    [InlineData("zh-HK", LanguagePreference.English)]
    [InlineData("zh-MO", LanguagePreference.English)]
    [InlineData("zh-Hant", LanguagePreference.English)]
    [InlineData("zh-CN", LanguagePreference.SimplifiedChinese)]
    [InlineData("zh-SG", LanguagePreference.SimplifiedChinese)]
    [InlineData("zh-Hans", LanguagePreference.SimplifiedChinese)]
    public void ResolveSystemUsesSupportedWindowsCultureOrEnglishFallback(
        string cultureName,
        LanguagePreference expected)
    {
        Assert.Equal(
            expected,
            LanguagePreferenceResolver.Resolve(
                LanguagePreference.System,
                CultureInfo.GetCultureInfo(cultureName)));
    }

    [Theory]
    [InlineData(LanguagePreference.English, "zh-CN")]
    [InlineData(LanguagePreference.SimplifiedChinese, "en-US")]
    [InlineData(LanguagePreference.Russian, "en-US")]
    [InlineData(LanguagePreference.English, "ru-RU")]
    public void ResolveExplicitPreferenceOverridesWindowsCulture(
        LanguagePreference preference,
        string cultureName)
    {
        Assert.Equal(
            preference,
            LanguagePreferenceResolver.Resolve(
                preference,
                CultureInfo.GetCultureInfo(cultureName)));
    }
}
