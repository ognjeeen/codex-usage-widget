using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Tests;

public sealed partial class LocalizationResourceTests
{
    private static readonly ResourceManager Resources = new(
        "CodexUsageWidget.Resources.Strings",
        typeof(Strings).Assembly);

    [Theory]
    [InlineData("zh-CN")]
    [InlineData("ru-RU")]
    public void TranslationsHaveTheSameKeysAndPlaceholdersAsEnglish(string cultureName)
    {
        var english = ReadResources(CultureInfo.InvariantCulture);
        var translation = ReadResources(CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(english.Keys.Order(), translation.Keys.Order());
        foreach (var key in english.Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(translation[key]), key);
            Assert.Equal(
                ReadPlaceholders(english[key]),
                ReadPlaceholders(translation[key]));
        }
    }

    [Fact]
    public void ResourceManagerUsesChineseAndFallsBackToEnglish()
    {
        Assert.Equal(
            "设置",
            Resources.GetString("Common_Settings", CultureInfo.GetCultureInfo("zh-CN")));
        Assert.Equal(
            "Настройки",
            Resources.GetString("Common_Settings", CultureInfo.GetCultureInfo("ru-RU")));
        Assert.Equal(
            "Settings",
            Resources.GetString("Common_Settings", CultureInfo.GetCultureInfo("fr-FR")));
    }

    private static Dictionary<string, string> ReadResources(CultureInfo culture)
    {
        var resourceSet = Resources.GetResourceSet(
            culture,
            createIfNotExists: true,
            tryParents: false);
        Assert.NotNull(resourceSet);
        return resourceSet.Cast<DictionaryEntry>().ToDictionary(
            entry => (string)entry.Key,
            entry => (string)entry.Value!);
    }

    private static string[] ReadPlaceholders(string value) => PlaceholderRegex()
        .Matches(value)
        .Select(match => match.Groups[1].Value)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [GeneratedRegex(@"\{(\d+)(?:[^}]*)\}")]
    private static partial Regex PlaceholderRegex();
}
