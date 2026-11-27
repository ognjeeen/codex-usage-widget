using System.Globalization;
using CodexUsageWidget.Application;

namespace CodexUsageWidget.Tests;

public sealed class TimeTextFormatterTests
{
    [Theory]
    [InlineData(TimeFormatPreference.Automatic, "en-US", "2:05 PM")]
    [InlineData(TimeFormatPreference.Automatic, "zh-CN", "14:05")]
    [InlineData(TimeFormatPreference.TwentyFourHour, "en-US", "14:05")]
    [InlineData(TimeFormatPreference.TwelveHour, "zh-CN", "下午 2:05")]
    [InlineData(TimeFormatPreference.TwelveHour, "ru-RU", "2:05 PM")]
    public void FormatTimeHonorsPreference(
        TimeFormatPreference preference,
        string cultureName,
        string expected)
    {
        var value = new DateTimeOffset(2030, 8, 31, 14, 5, 0, TimeSpan.Zero);

        var result = TimeTextFormatter.FormatTime(
            value,
            preference,
            CultureInfo.GetCultureInfo(cultureName),
            CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(TimeFormatPreference.Automatic, "en-US", "2:05:09 PM")]
    [InlineData(TimeFormatPreference.TwentyFourHour, "en-US", "14:05:09")]
    [InlineData(TimeFormatPreference.TwelveHour, "zh-CN", "下午 2:05:09")]
    [InlineData(TimeFormatPreference.TwelveHour, "ru-RU", "2:05:09 PM")]
    public void FormatTimeWithSecondsHonorsPreference(
        TimeFormatPreference preference,
        string cultureName,
        string expected)
    {
        var value = new DateTimeOffset(2030, 8, 31, 14, 5, 9, TimeSpan.Zero);

        var result = TimeTextFormatter.FormatTimeWithSeconds(
            value,
            preference,
            CultureInfo.GetCultureInfo(cultureName),
            CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDayAndTimeUsesPreferredClock()
    {
        var value = new DateTimeOffset(2030, 8, 31, 14, 5, 0, TimeSpan.Zero);

        var result = TimeTextFormatter.FormatDayAndTime(
            value,
            TimeFormatPreference.TwelveHour,
            CultureInfo.GetCultureInfo("zh-CN"));

        Assert.Equal("周六 下午 2:05", result);
    }

    [Fact]
    public void ExplicitTwelveHourFormatShowsPeriodWhenRegionalDesignatorsAreEmpty()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("ru-RU").Clone();
        culture.DateTimeFormat.AMDesignator = string.Empty;
        culture.DateTimeFormat.PMDesignator = string.Empty;
        var value = new DateTimeOffset(2030, 8, 31, 14, 5, 0, TimeSpan.Zero);

        Assert.Equal("2:05 PM", TimeTextFormatter.FormatTime(
            value, TimeFormatPreference.TwelveHour, culture));
    }
}
