using System.Diagnostics;

namespace CodexUsageWidget.Infrastructure.Windows;

internal static class GitHubReleaseLauncher
{
    private const string LatestReleaseUrl =
        "https://github.com/ognjeeen/codex-usage-widget/releases/latest";

    public static void OpenLatestRelease()
    {
        var startInfo = new ProcessStartInfo(LatestReleaseUrl)
        {
            UseShellExecute = true
        };

        if (Process.Start(startInfo) is null)
        {
            throw new InvalidOperationException("Windows could not open the widget release page.");
        }
    }
}
