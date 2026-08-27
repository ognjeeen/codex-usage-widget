using CodexUsageWidget.Infrastructure.Settings;

namespace CodexUsageWidget.Tests;

public sealed class AccentPaletteStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(AccentPalette.Blue)]
    [InlineData(AccentPalette.Violet)]
    [InlineData(AccentPalette.Teal)]
    [InlineData(AccentPalette.Emerald)]
    [InlineData(AccentPalette.Pink)]
    public void SavePersistsPalette(AccentPalette palette)
    {
        var store = new AccentPaletteStore(Path.Combine(_directory, "accent-palette.txt"));

        store.Save(palette);

        Assert.Equal(palette, store.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
