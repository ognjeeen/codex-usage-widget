using System.Globalization;
using CodexUsageWidget.Application;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Tests;

[Collection("Localization")]
public sealed class UsageTextFormatterTests : IDisposable
{
    [Theory]
    [InlineData("codex executable not found", "Codex CLI was not found on PATH.")]
    [InlineData("unauthorized: login required", "Run codex login, then refresh.")]
    public void ToFriendlyErrorMapsCommonFailures(string input, string expected) =>
        Assert.Equal(expected, UsageTextFormatter.ToFriendlyError(input));

    [Fact]
    public void FormatResetUsesCeilingForRemainingHours()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("en-US"));
        var now = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var reset = now.AddHours(2).AddMinutes(10);

        var result = UsageTextFormatter.FormatReset(
            reset,
            now,
            TimeFormatPreference.TwelveHour);

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
    [InlineData(0, "#FF5A5F")]
    [InlineData(5, "#FF5A5F")]
    [InlineData(6, "#F6C453")]
    [InlineData(25, "#F6C453")]
    [InlineData(26, "#4ADE80")]
    public void ColorForRemainingUsesThresholds(double remaining, string expected) =>
        Assert.Equal(expected, UsageTextFormatter.ColorForRemaining(remaining));

    public void Dispose() =>
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("en-US"));
}
