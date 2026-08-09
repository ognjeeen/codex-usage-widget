namespace CodexUsageWidget.Infrastructure.Windows;

public interface IStartupRegistrationStore
{
    string? LoadCommand();

    void SaveCommand(string command);

    void DeleteCommand();
}
