using CodexUsageWidget.Infrastructure.Preview;

namespace CodexUsageWidget.Tests;

public sealed class PreviewStartupRegistrationStoreTests
{
    [Fact]
    public void StartupRegistrationDoesNotPersistOutsideThePreviewInstance()
    {
        var previewStore = new PreviewStartupRegistrationStore();

        previewStore.SaveCommand(@"C:\Preview\CodexUsageWidget.exe");

        Assert.Equal(@"C:\Preview\CodexUsageWidget.exe", previewStore.LoadCommand());
        Assert.Null(new PreviewStartupRegistrationStore().LoadCommand());
    }
}
