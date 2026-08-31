using System.IO;
using System.Text;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Infrastructure.Codex.Hooks;

public static class CodexActivityCommandLine
{
    private const string HookArgument = "--codex-activity-hook";
    private const string InstallArgument = "--install-activity-hooks";
    private const string UninstallArgument = "--uninstall-activity-hooks";

    public static bool IsCommandMode(IReadOnlyList<string> arguments) =>
        arguments.Count == 1 &&
        arguments[0] is HookArgument or InstallArgument or UninstallArgument;

    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        await using var inputStream = Console.OpenStandardInput();
        await using var outputStream = Console.OpenStandardOutput();

        if (arguments[0] == HookArgument)
        {
            return await CodexActivityHookCommand.RunAsync(
                inputStream,
                outputStream,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        _ = new AppLanguageController(new LanguagePreferenceStore());

        using var input = new StreamReader(
            inputStream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);
        await using var output = new StreamWriter(
            outputStream,
            new UTF8Encoding(false),
            leaveOpen: true)
        {
            AutoFlush = true
        };

        return await RunInteractiveAsync(
            arguments,
            input,
            output,
            new CodexHookConfigurationManager(),
            cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<int> RunInteractiveAsync(
        IReadOnlyList<string> arguments,
        TextReader input,
        TextWriter output,
        CodexHookConfigurationManager manager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var install = arguments[0] == InstallArgument;
            var plan = install
                ? manager.PlanInstall()
                : manager.PlanUninstall();
            if (plan.Error is not null)
            {
                var error = plan.ErrorKind == CodexHookConfigurationErrorKind.HooksDisabled
                    ? Strings.Get("Activity_HooksDisabledDescription")
                    : Strings.Format("Activity_CommandFailure", plan.Error);
                await output.WriteLineAsync(error).ConfigureAwait(false);
                return 1;
            }

            if (!plan.HasChanges)
            {
                await output.WriteLineAsync(
                    install
                        ? Strings.Get("Activity_CommandAlreadyInstalled")
                        : Strings.Get("Activity_CommandNoMatching")).ConfigureAwait(false);
                return 0;
            }

            await output.WriteLineAsync(Strings.Get("Activity_CommandProposed"))
                .ConfigureAwait(false);
            await output.WriteLineAsync(plan.ProposedContent).ConfigureAwait(false);
            await output.WriteAsync(Strings.Get("Activity_CommandApplyPrompt"))
                .ConfigureAwait(false);
            var approval = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(approval, "y", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(approval, "yes", StringComparison.OrdinalIgnoreCase))
            {
                await output.WriteLineAsync(Strings.Get("Activity_CommandNoChanges"))
                    .ConfigureAwait(false);
                return 0;
            }

            manager.Apply(plan);
            await output.WriteLineAsync(
                install
                    ? Strings.Get("Activity_CommandInstalled")
                    : Strings.Get("Activity_CommandRemoved")).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await output.WriteLineAsync(Strings.Format("Activity_CommandFailure", ex.Message))
                .ConfigureAwait(false);
            return 1;
        }
    }
}
