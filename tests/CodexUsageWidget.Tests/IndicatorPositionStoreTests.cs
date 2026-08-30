using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class IndicatorPositionStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void LoadsBottomLeftWhenNoSavedPositionExists()
    {
        var store = new IndicatorPositionStore(Path.Combine(_directory, "indicator-position.txt"));

        Assert.Equal(IndicatorPosition.BottomLeft, store.Load());
    }

    [Fact]
    public void SavesAndClampsTheSelectedPosition()
    {
        var store = new IndicatorPositionStore(Path.Combine(_directory, "indicator-position.txt"));

        store.Save(new IndicatorPosition(121, -4));

        Assert.Equal(new IndicatorPosition(100, 0), store.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}