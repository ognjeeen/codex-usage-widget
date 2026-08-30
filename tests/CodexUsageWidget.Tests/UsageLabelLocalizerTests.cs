using System.Globalization;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Tests;

public sealed class UsageLabelLocalizerTests
{
    [Theory]
    [InlineData("Weekly limit", "每周限制")]
    [InlineData("1d limit", "1 天限制")]
    [InlineData("6h limit", "6 小时限制")]
    [InlineData("30m limit", "30 分钟限制")]
    [InlineData("Primary limit", "主要限制")]
    [InlineData("Secondary limit", "次要限制")]
    [InlineData("GPT-5.3-Codex", "GPT-5.3-Codex")]
    public void LocalizeUsesChineseForKnownPresentationLabels(
        string label,
        string expected) => Assert.Equal(
            expected,
            UsageLabelLocalizer.Localize(label, CultureInfo.GetCultureInfo("zh-CN")));
}
