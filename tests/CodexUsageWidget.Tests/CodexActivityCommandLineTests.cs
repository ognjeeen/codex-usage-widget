using System.Globalization;
using CodexUsageWidget.Infrastructure.Codex.Hooks;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Tests;

[Collection("Localization")]
public sealed class CodexActivityCommandLineTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "CodexUsageWidget.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task UninstallWithoutMatchingHooksUsesSelectedLanguage()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("zh-CN"));
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var manager = new CodexHookConfigurationManager(
            Path.Combine(_directory, "hooks.json"),
            Path.Combine(_directory, "config.toml"));

        var exitCode = await CodexActivityCommandLine.RunInteractiveAsync(
            ["--uninstall-activity-hooks"],
            TextReader.Null,
            output,
            manager);

        Assert.Equal(0, exitCode);
        Assert.Equal("未找到已安装的匹配活动钩子。", output.ToString().Trim());
    }

    [Fact]
    public async Task DeclinedInstallUsesSelectedLanguage()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("zh-CN"));
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var manager = new CodexHookConfigurationManager(
            Path.Combine(_directory, "hooks.json"),
            Path.Combine(_directory, "config.toml"));

        var exitCode = await CodexActivityCommandLine.RunInteractiveAsync(
            ["--install-activity-hooks"],
            new StringReader("n"),
            output,
            manager);

        Assert.Equal(0, exitCode);
        Assert.Contains("拟写入的 ~/.codex/hooks.json：", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("应用此更改？[y/N] ", output.ToString(), StringComparison.Ordinal);
        Assert.EndsWith("未进行任何更改。", output.ToString().Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuccessfulInstallUsesSelectedLanguage()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("zh-CN"));
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var hooksPath = Path.Combine(_directory, "hooks.json");
        var manager = new CodexHookConfigurationManager(
            hooksPath,
            Path.Combine(_directory, "config.toml"));

        var exitCode = await CodexActivityCommandLine.RunInteractiveAsync(
            ["--install-activity-hooks"],
            new StringReader("y"),
            output,
            manager);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(hooksPath));
        Assert.EndsWith(
            "活动钩子已安装。请使用 /hooks 检查并信任确切定义。",
            output.ToString().Trim(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuccessfulUninstallUsesSelectedLanguage()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("zh-CN"));
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var manager = new CodexHookConfigurationManager(
            Path.Combine(_directory, "hooks.json"),
            Path.Combine(_directory, "config.toml"));
        await CodexActivityCommandLine.RunInteractiveAsync(
            ["--install-activity-hooks"],
            new StringReader("y"),
            output,
            manager);
        output.GetStringBuilder().Clear();

        var exitCode = await CodexActivityCommandLine.RunInteractiveAsync(
            ["--uninstall-activity-hooks"],
            new StringReader("y"),
            output,
            manager);

        Assert.Equal(0, exitCode);
        Assert.EndsWith(
            "已移除匹配的活动钩子。",
            output.ToString().Trim(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisabledHooksErrorUsesSelectedLanguage()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("zh-CN"));
        Directory.CreateDirectory(_directory);
        var configPath = Path.Combine(_directory, "config.toml");
        await File.WriteAllTextAsync(configPath, "[features]\nhooks = false\n");
        var output = new StringWriter(CultureInfo.InvariantCulture);
        var manager = new CodexHookConfigurationManager(
            Path.Combine(_directory, "hooks.json"),
            configPath);

        var exitCode = await CodexActivityCommandLine.RunInteractiveAsync(
            ["--install-activity-hooks"],
            TextReader.Null,
            output,
            manager);

        Assert.Equal(1, exitCode);
        Assert.Equal(
            "安装前，请在 Codex 配置的 [features] 部分设置 hooks = true。",
            output.ToString().Trim());
    }

    public void Dispose()
    {
        Strings.Current.SetCulture(CultureInfo.GetCultureInfo("en-US"));
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
