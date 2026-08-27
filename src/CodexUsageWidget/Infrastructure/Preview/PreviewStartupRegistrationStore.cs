using CodexUsageWidget.Infrastructure.Windows;

namespace CodexUsageWidget.Infrastructure.Preview;

public sealed class PreviewStartupRegistrationStore : IStartupRegistrationStore
{
    private string? _command;

    public string? LoadCommand() => _command;

    public void SaveCommand(string command) => _command = command;

    public void DeleteCommand() => _command = null;
}
