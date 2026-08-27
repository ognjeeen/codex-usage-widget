using CodexUsageWidget.Infrastructure.Windows;
using Microsoft.Win32;

namespace CodexUsageWidget.Tests;

public sealed class WindowsThemeMonitorTests : IDisposable
{
    private readonly string _keyPath = $@"Software\CodexUsageWidget.Tests\{Guid.NewGuid():N}";

    [Fact]
    public void ReadsAppAndWindowsThemeSettingsIndependently()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(_keyPath))
        {
            key.SetValue("AppsUseLightTheme", 1, RegistryValueKind.DWord);
            key.SetValue("SystemUsesLightTheme", 0, RegistryValueKind.DWord);
        }

        using var monitor = new WindowsThemeMonitor(_keyPath);

        Assert.True(monitor.UsesLightAppTheme);
        Assert.False(monitor.UsesLightSystemTheme);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);
    }
}
