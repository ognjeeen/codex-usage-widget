using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using CodexUsageWidget.Views;

namespace CodexUsageWidget.Tests;

public sealed class TaskbarSettingsMenuTests
{
    [Fact]
    public void SettingsMenuOffersUpdateCheck()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var application = new App();
                application.InitializeComponent();
                var window = new TaskbarLabelWindow();
                var menu = Assert.IsType<ContextMenu>(window.FindName("TaskbarMenu"));

                Assert.Contains(
                    menu.Items.OfType<MenuItem>(),
                    item => string.Equals(
                        item.Header as string,
                        "Check for updates...",
                        StringComparison.Ordinal));

                window.Close();
                application.Shutdown();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
