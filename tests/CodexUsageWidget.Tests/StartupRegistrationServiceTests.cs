using CodexUsageWidget.Infrastructure.Windows;

namespace CodexUsageWidget.Tests;

public sealed class StartupRegistrationServiceTests
{
    private const string ExecutablePath = @"C:\Apps\Codex Usage Widget\CodexUsageWidget.exe";
    private const string StartupCommand = "\"C:\\Apps\\Codex Usage Widget\\CodexUsageWidget.exe\"";

    [Fact]
    public void MissingRegistrationIsDisabled()
    {
        var service = new StartupRegistrationService(ExecutablePath, new FakeStore());

        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void EnablingWritesQuotedExecutablePath()
    {
        var store = new FakeStore();
        var service = new StartupRegistrationService(ExecutablePath, store);

        var result = service.TrySetEnabled(enabled: true);

        Assert.True(result);
        Assert.Equal(StartupCommand, store.Command);
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void DisablingDeletesRegistration()
    {
        var store = new FakeStore { Command = StartupCommand };
        var service = new StartupRegistrationService(ExecutablePath, store);

        var result = service.TrySetEnabled(enabled: false);

        Assert.True(result);
        Assert.Null(store.Command);
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void RefreshUpdatesMovedPortableExecutablePath()
    {
        var store = new FakeStore { Command = "\"C:\\Old location\\CodexUsageWidget.exe\"" };
        var service = new StartupRegistrationService(ExecutablePath, store);

        var result = service.TryRefreshExecutablePathIfEnabled();

        Assert.True(result);
        Assert.Equal(StartupCommand, store.Command);
    }

    [Fact]
    public void RefreshDoesNotCreateMissingRegistration()
    {
        var store = new FakeStore();
        var service = new StartupRegistrationService(ExecutablePath, store);

        var result = service.TryRefreshExecutablePathIfEnabled();

        Assert.True(result);
        Assert.Null(store.Command);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void RegistryFailureDoesNotEscapeIntoTheUi()
    {
        var store = new FakeStore { WriteException = new UnauthorizedAccessException() };
        var service = new StartupRegistrationService(ExecutablePath, store);

        var result = service.TrySetEnabled(enabled: true);

        Assert.False(result);
        Assert.False(service.IsEnabled);
    }

    private sealed class FakeStore : IStartupRegistrationStore
    {
        public string? Command { get; set; }

        public Exception? WriteException { get; init; }

        public int SaveCount { get; private set; }

        public string? LoadCommand() => Command;

        public void SaveCommand(string command)
        {
            if (WriteException is not null)
            {
                throw WriteException;
            }

            Command = command;
            SaveCount++;
        }

        public void DeleteCommand() => Command = null;
    }
}
