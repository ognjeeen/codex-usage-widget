using System.Globalization;
using CodexUsageWidget.Application;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Tests;

[Collection("Localization")]
public sealed class UsageTextFormatterTests : IDisposable
{
    private readonly CultureInfo _originalWindowsRegionalCulture =
        Strings.Current.WindowsRegionalCulture;

    [Theory]
    [InlineData("codex executable not found", "Codex CLI was not found on PATH.")]
    [InlineData("unauthorized: login required", "Run codex login, then refresh.")]
    public void ToFriendlyErrorMapsCommonFailures(string input, string expected) =>
        Assert.Equal(expected, UsageTextFormatter.ToFriendlyError(input));

    [Fact]
    public void FormatResetUsesCeilingForRemainingHours()
    {
        var english = CultureInfo.GetCultureInfo("en-US");
        Strings.Current.SetCulture(english, english);
        var now = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reset = now.AddHours(2).AddMinutes(10);

        var result = UsageTextFormatter.FormatReset(reset, now);

        Assert.Equal("Resets in 3h · 2:10 PM", result);
    }

    [Fact]
    public void FormatResetHonorsTwentyFourHourPreference()
    {
        var now = new DateTimeOffset(2030, 8, 31, 12, 0, 0, TimeSpan.Zero);
        var reset = now.AddHours(2).AddMinutes(10);

        var result = UsageTextFormatter.FormatReset(
            reset,
            now,
            TimeFormatPreference.TwentyFourHour);

        Assert.Equal("Resets in 3h · 14:10", result);
    }

    [Theory]
    [InlineData(10, "#E16D76")]
    [InlineData(25, "#DDA56D")]
    [InlineData(26, "#E7E7E7")]
    public void ColorForRemainingUsesThresholds(double remaining, string expected) =>
        Assert.Equal(expected, UsageTextFormatter.ColorForRemaining(remaining));

    public void Dispose() =>
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("en-US"), _originalWindowsRegionalCulture);
}
