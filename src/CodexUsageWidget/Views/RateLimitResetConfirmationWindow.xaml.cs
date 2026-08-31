using System.Windows;
using System.Windows.Input;
using CodexUsageWidget.Application;
using CodexUsageWidget.Localization;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Views;

public partial class RateLimitResetConfirmationWindow : Window
{
    public RateLimitResetConfirmationWindow(
        RateLimitResetCreditViewModel credit,
        TimeFormatPreference timeFormatPreference = TimeFormatPreference.Automatic)
    {
        ArgumentNullException.ThrowIfNull(credit);
        InitializeComponent();
        DescriptionText.Text = credit.ExpiresAt is { } expiresAt
            ? Strings.Format(
                "Usage_ResetConfirmKnown",
                expiresAt,
                TimeTextFormatter.FormatTime(expiresAt, timeFormatPreference))
            : credit.CreditId is not null
                ? Strings.Get("Usage_ResetConfirmUnknownExpiration")
                : Strings.Get("Usage_ResetConfirmNext");
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void UseResetButton_OnClick(object sender, RoutedEventArgs e) =>
        DialogResult = true;
}
