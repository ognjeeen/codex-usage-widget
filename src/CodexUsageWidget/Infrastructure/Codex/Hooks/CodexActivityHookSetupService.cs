using CodexUsageWidget.Application;

namespace CodexUsageWidget.Infrastructure.Codex.Hooks;

public sealed class CodexActivityHookSetupService : IActivityHookSetupService
{
    private readonly CodexHookConfigurationManager _configurationManager;
    private readonly ICodexAppServerSession _session;
    private readonly string _workingDirectory;

    public CodexActivityHookSetupService(
        CodexHookConfigurationManager configurationManager,
        ICodexAppServerSession session,
        string? workingDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(configurationManager);
        ArgumentNullException.ThrowIfNull(session);
        _configurationManager = configurationManager;
        _session = session;
        _workingDirectory = workingDirectory ?? Environment.CurrentDirectory;
    }

    public async Task<ActivityHookSetupStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var configurationPlan = _configurationManager.PlanInstall();
        if (configurationPlan.Error is not null)
        {
            var hooksDisabled = configurationPlan.ErrorKind ==
                CodexHookConfigurationErrorKind.HooksDisabled;
            var hasInstalledHandlers = false;
            if (hooksDisabled)
            {
                var uninstallPlan = _configurationManager.PlanUninstall();
                hasInstalledHandlers = uninstallPlan.Error is null && uninstallPlan.HasChanges;
            }

            return new ActivityHookSetupStatus(
                hooksDisabled
                    ? ActivityHookSetupState.HooksDisabled
                    : ActivityHookSetupState.Error,
                configurationPlan.Error,
                hasInstalledHandlers);
        }

        if (configurationPlan.HasChanges)
        {
            return new ActivityHookSetupStatus(
                configurationPlan.HasRecognizedHandlers
                    ? ActivityHookSetupState.UpdateRequired
                    : ActivityHookSetupState.NotInstalled);
        }

        try
        {
            var result = await _session.RequestAsync(
                    "hooks/list",
                    new { cwds = new[] { _workingDirectory } },
                    cancellationToken)
                .ConfigureAwait(false);
            return FromTrustEvaluation(CodexHookTrustStatusParser.Parse(
                result,
                CodexActivityHookBridge.Command));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ActivityHookSetupStatus(
                ActivityHookSetupState.InstalledStatusUnavailable,
                ex.Message);
        }
    }

    public ActivityHookChangePreview PrepareChange(ActivityHookChangeKind kind)
    {
        var plan = CreatePlan(kind);
        EnsureValid(plan);
        return new ActivityHookChangePreview(kind, plan.HasChanges, plan.ProposedContent);
    }

    public void ApplyChange(ActivityHookChangePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var currentPlan = CreatePlan(preview.Kind);
        EnsureValid(currentPlan);

        if (currentPlan.HasChanges != preview.HasChanges ||
            !string.Equals(
                currentPlan.ProposedContent,
                preview.ProposedContent,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Codex hooks changed after the preview. Review the updated change and try again.");
        }

        _configurationManager.Apply(currentPlan);
    }

    private CodexHookConfigurationPlan CreatePlan(ActivityHookChangeKind kind) =>
        kind == ActivityHookChangeKind.Install
            ? _configurationManager.PlanInstall()
            : _configurationManager.PlanUninstall();

    private static void EnsureValid(CodexHookConfigurationPlan plan)
    {
        if (plan.Error is not null)
        {
            throw new InvalidOperationException(plan.Error);
        }
    }

    private static ActivityHookSetupStatus FromTrustEvaluation(
        CodexHookTrustEvaluation evaluation) =>
        evaluation switch
        {
            CodexHookTrustEvaluation.ApprovalRequired =>
                new ActivityHookSetupStatus(ActivityHookSetupState.ApprovalRequired),
            CodexHookTrustEvaluation.Active =>
                new ActivityHookSetupStatus(ActivityHookSetupState.Active),
            CodexHookTrustEvaluation.Modified =>
                new ActivityHookSetupStatus(ActivityHookSetupState.Modified),
            _ => new ActivityHookSetupStatus(ActivityHookSetupState.InstalledStatusUnavailable)
        };
}
