using System.Windows;
using System.Windows.Media;
using CodexUsageWidget.Views.ViewModels;

namespace CodexUsageWidget.Views.Controls;

public partial class UsagePaceChart : System.Windows.Controls.UserControl
{
    private const double LogicalWidth = 280d;
    private const double LogicalHeight = 42d;

    public UsagePaceChart()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => HideHover();
        Unloaded += (_, _) => HideHover();
    }

    private void PlotHost_OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (DataContext is not UsageLimitPaceViewModel viewModel ||
            PlotHost.ActualWidth <= 0d ||
            PlotHost.ActualHeight <= 0d)
        {
            HideHover();
            return;
        }

        var pointer = e.GetPosition(PlotHost);
        var logicalX = pointer.X * LogicalWidth / PlotHost.ActualWidth;
        var index = UsagePaceHoverSelector.FindNearestIndex(viewModel.ChartPoints, logicalX);
        if (index < 0 || index >= viewModel.History.Count)
        {
            HideHover();
            return;
        }

        var point = viewModel.ChartPoints[index];
        var x = point.X * PlotHost.ActualWidth / LogicalWidth;
        var y = point.Y * PlotHost.ActualHeight / LogicalHeight;
        HoverGuide.X1 = x;
        HoverGuide.X2 = x;
        HoverGuide.Y2 = PlotHost.ActualHeight;
        System.Windows.Controls.Canvas.SetLeft(HoverMarker, x - HoverMarker.Width / 2d);
        System.Windows.Controls.Canvas.SetTop(HoverMarker, y - HoverMarker.Height / 2d);
        HoverReadout.Text = viewModel.History[index].ToolTipText;
        HoverGuide.Visibility = Visibility.Visible;
        HoverMarker.Visibility = Visibility.Visible;
        HoverReadout.Visibility = Visibility.Visible;
    }

    private void PlotHost_OnMouseLeave(
        object sender,
        System.Windows.Input.MouseEventArgs e) => HideHover();

    private void HideHover()
    {
        HoverGuide.Visibility = Visibility.Collapsed;
        HoverMarker.Visibility = Visibility.Collapsed;
        HoverReadout.Visibility = Visibility.Hidden;
    }
}
