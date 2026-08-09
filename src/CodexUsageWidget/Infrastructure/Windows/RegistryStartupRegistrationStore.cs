using System.IO;
using Microsoft.Win32;

namespace CodexUsageWidget.Infrastructure.Windows;

public sealed class RegistryStartupRegistrationStore : IStartupRegistrationStore
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CodexUsageWidget";

    public string? LoadCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(
            ValueName,
            defaultValue: null,
            RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
    }

    public void SaveCommand(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true) ??
            throw new IOException("The current-user Windows startup key could not be opened.");
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    public void DeleteCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
