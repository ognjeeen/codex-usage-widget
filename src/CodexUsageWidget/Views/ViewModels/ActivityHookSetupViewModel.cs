using System.Windows.Media;
using CodexUsageWidget.Application;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Views.ViewModels;

public sealed class ActivityHookSetupViewModel
{
    private ActivityHookSetupViewModel()
    {
    }

    public string StatusLabel { get; private init; } = Strings.Get("Activity_Checking");

    public string Description { get; private init; } =
        Strings.Get("Activity_CheckingDescription");

    public System.Windows.Media.Brush StatusBrush { get; private init; } =
        BrushFromHex("#D6A15F");

    public bool CanInstall { get; private init; }

    public bool CanOpenCodex { get; private init; }

    public bool CanUninstall { get; private init; }

    public bool CanRefresh { get; private init; }

    public string CodexActionLabel { get; private init; } = Strings.Get("Activity_OpenInCodex");

    public string InstallActionLabel { get; private init; } = Strings.Get("Activity_ReviewHooks");

    public static ActivityHookSetupViewModel Loading() => new();

    public static ActivityHookSetupViewModel FromStatus(ActivityHookSetupStatus status) =>
        status.State switch
        {
            ActivityHookSetupState.NotInstalled => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_SetupRequired"),
                Description = Strings.Get("Activity_SetupRequiredDescription"),
                StatusBrush = BrushFromHex("#D6A15F"),
                CanInstall = true,
                CanRefresh = true
            },
            ActivityHookSetupState.UpdateRequired => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_UpdateAvailable"),
                Description = Strings.Get("Activity_UpdateDescription"),
                StatusBrush = BrushFromHex("#D6A15F"),
                InstallActionLabel = Strings.Get("Activity_ReviewUpdate"),
                CanInstall = true,
                CanUninstall = true,
                CanRefresh = true
            },
            ActivityHookSetupState.ApprovalRequired => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_OneStepLeft"),
                Description = Strings.Get("Activity_ApprovalDescription"),
                StatusBrush = BrushFromHex("#D6A15F"),
                CodexActionLabel = Strings.Get("Activity_ApproveInCodex"),
                CanOpenCodex = true,
                CanUninstall = true,
                CanRefresh = true
            },
            ActivityHookSetupState.Active => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_Ready"),
                Description = Strings.Get("Activity_ReadyDescription"),
                StatusBrush = BrushFromHex("#68B88A"),
                CanUninstall = true,
                CanRefresh = true
            },
            ActivityHookSetupState.Modified => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_ReviewRequired"),
                Description = Strings.Get("Activity_ModifiedDescription"),
                StatusBrush = BrushFromHex("#D6A15F"),
                CodexActionLabel = Strings.Get("Activity_ReviewInCodex"),
                CanOpenCodex = true,
                CanUninstall = true,
                CanRefresh = true
            },
            ActivityHookSetupState.HooksDisabled => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_HooksDisabled"),
                Description = Strings.Get("Activity_HooksDisabledDescription"),
                StatusBrush = BrushFromHex("#E16D76"),
                CanUninstall = status.HasInstalledHandlers,
                CanRefresh = true
            },
            ActivityHookSetupState.InstalledStatusUnavailable => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_StatusUnavailable"),
                Description = status.Detail is null
                    ? Strings.Get("Activity_StatusUnavailableDescription")
                    : Strings.Format("Activity_StatusUnavailableWithDetail", status.Detail),
                StatusBrush = BrushFromHex("#D6A15F"),
                CodexActionLabel = Strings.Get("Activity_OpenHooksInCodex"),
                CanOpenCodex = true,
                CanUninstall = true,
                CanRefresh = true
            },
            _ => new ActivityHookSetupViewModel
            {
                StatusLabel = Strings.Get("Activity_SetupUnavailable"),
                Description = status.Detail is null
                    ? Strings.Get("Activity_SetupUnavailableDescription")
                    : Strings.Format("Activity_SetupErrorDetail", status.Detail),
                StatusBrush = BrushFromHex("#E16D76"),
                CanRefresh = true
            }
        };

    public static ActivityHookSetupViewModel Error(string message) => new()
    {
        StatusLabel = Strings.Get("Activity_SetupUnavailable"),
        Description = message,
        StatusBrush = BrushFromHex("#E16D76"),
        CanRefresh = true
    };

    private static SolidColorBrush BrushFromHex(string color) =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
}
