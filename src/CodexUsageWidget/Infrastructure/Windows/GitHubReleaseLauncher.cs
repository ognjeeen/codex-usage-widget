using System.ComponentModel;
using System.Diagnostics;

namespace CodexUsageWidget.Infrastructure.Windows;

internal static class GitHubReleaseLauncher
{
    private const string LatestReleaseUrl =
        "https://github.com/ognjeeen/codex-usage-widget/releases/latest";

    public static bool TryOpenLatestRelease()
    {
        try
        {
            var startInfo = new ProcessStartInfo(LatestReleaseUrl)
            {
                UseShellExecute = true
            };

            return Process.Start(startInfo) is not null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
