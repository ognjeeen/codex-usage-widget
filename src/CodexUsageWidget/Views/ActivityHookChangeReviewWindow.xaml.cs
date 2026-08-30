using System.Windows;
using System.Windows.Input;
using CodexUsageWidget.Application;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Views;

public partial class ActivityHookChangeReviewWindow : Window
{
    public ActivityHookChangeReviewWindow(ActivityHookChangePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        InitializeComponent();

        var installing = preview.Kind == ActivityHookChangeKind.Install;
        HeadingText.Text = installing
            ? Strings.Get("Activity_ReviewInstallHeading")
            : Strings.Get("Activity_ReviewRemoveHeading");
        DescriptionText.Text = installing
            ? Strings.Get("Activity_ReviewInstallDescription")
            : Strings.Get("Activity_ReviewRemoveDescription");
        ProposedContentText.Text = preview.ProposedContent;
        ApplyButton.Content = installing
            ? Strings.Get("Activity_InstallHooks")
            : Strings.Get("Activity_RemoveHooks");
        if (!installing)
        {
            ApplyButton.Style = (Style)FindResource("DangerDialogButton");
        }
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
