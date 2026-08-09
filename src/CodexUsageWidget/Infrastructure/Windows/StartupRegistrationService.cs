using System.IO;
using System.Security;

namespace CodexUsageWidget.Infrastructure.Windows;

public sealed class StartupRegistrationService
{
    private readonly IStartupRegistrationStore _store;
    private readonly string _startupCommand;

    public StartupRegistrationService(
        string executablePath,
        IStartupRegistrationStore? store = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        _store = store ?? new RegistryStartupRegistrationStore();
        _startupCommand = $"\"{Path.GetFullPath(executablePath)}\"";
    }

    public bool IsEnabled
    {
        get
        {
            try
            {
                return !string.IsNullOrWhiteSpace(_store.LoadCommand());
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
        }
    }

    public bool TrySetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                _store.SaveCommand(_startupCommand);
            }
            else
            {
                _store.DeleteCommand();
            }

            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }

    public bool TryRefreshExecutablePathIfEnabled()
    {
        try
        {
            var registeredCommand = _store.LoadCommand();
            if (string.IsNullOrWhiteSpace(registeredCommand) ||
                string.Equals(registeredCommand, _startupCommand, StringComparison.Ordinal))
            {
                return true;
            }

            _store.SaveCommand(_startupCommand);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (SecurityException)
        {
            return false;
        }
    }
}
