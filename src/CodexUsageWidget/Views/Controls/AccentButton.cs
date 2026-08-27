using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;

namespace CodexUsageWidget.Views.Controls;

public sealed class AccentButton : WpfButton
{
    private static MediaColor _primary = MediaColor.FromRgb(59, 120, 216);
    private static MediaColor _border = MediaColor.FromRgb(91, 145, 229);
    private static MediaColor _hover = MediaColor.FromRgb(74, 136, 232);
    private static MediaColor _hoverBorder = MediaColor.FromRgb(118, 173, 242);
    private static MediaColor _pressed = MediaColor.FromRgb(46, 99, 181);

    public static readonly DependencyProperty PrimaryBrushProperty = DependencyProperty.Register(
        nameof(PrimaryBrush),
        typeof(MediaBrush),
        typeof(AccentButton));

    public static readonly DependencyProperty PrimaryBorderBrushProperty =
        DependencyProperty.Register(
            nameof(PrimaryBorderBrush),
            typeof(MediaBrush),
            typeof(AccentButton));

    public static readonly DependencyProperty HoverBrushProperty = DependencyProperty.Register(
        nameof(HoverBrush),
        typeof(MediaBrush),
        typeof(AccentButton));

    public static readonly DependencyProperty HoverBorderBrushProperty =
        DependencyProperty.Register(
            nameof(HoverBorderBrush),
            typeof(MediaBrush),
            typeof(AccentButton));

    public static readonly DependencyProperty PressedBrushProperty = DependencyProperty.Register(
        nameof(PressedBrush),
        typeof(MediaBrush),
        typeof(AccentButton));

    private static event Action? PaletteChanged;

    public AccentButton()
    {
        Loaded += AccentButtonOnLoaded;
        Unloaded += AccentButtonOnUnloaded;
        ApplyCurrentPalette();
    }

    public MediaBrush HoverBrush
    {
        get => (MediaBrush)GetValue(HoverBrushProperty);
        private set => SetValue(HoverBrushProperty, value);
    }

    public MediaBrush PrimaryBrush
    {
        get => (MediaBrush)GetValue(PrimaryBrushProperty);
        private set => SetValue(PrimaryBrushProperty, value);
    }

    public MediaBrush PrimaryBorderBrush
    {
        get => (MediaBrush)GetValue(PrimaryBorderBrushProperty);
        private set => SetValue(PrimaryBorderBrushProperty, value);
    }

    public MediaBrush HoverBorderBrush
    {
        get => (MediaBrush)GetValue(HoverBorderBrushProperty);
        private set => SetValue(HoverBorderBrushProperty, value);
    }

    public MediaBrush PressedBrush
    {
        get => (MediaBrush)GetValue(PressedBrushProperty);
        private set => SetValue(PressedBrushProperty, value);
    }

    internal static void ApplyPalette(
        MediaColor primary,
        MediaColor border,
        MediaColor hover,
        MediaColor hoverBorder,
        MediaColor pressed)
    {
        _primary = primary;
        _border = border;
        _hover = hover;
        _hoverBorder = hoverBorder;
        _pressed = pressed;
        PaletteChanged?.Invoke();
    }

    private void AccentButtonOnLoaded(object sender, RoutedEventArgs e)
    {
        PaletteChanged -= ApplyCurrentPalette;
        PaletteChanged += ApplyCurrentPalette;
        ApplyCurrentPalette();
    }

    private void AccentButtonOnUnloaded(object sender, RoutedEventArgs e) =>
        PaletteChanged -= ApplyCurrentPalette;

    private void ApplyCurrentPalette()
    {
        PrimaryBrush = new SolidColorBrush(_primary);
        PrimaryBorderBrush = new SolidColorBrush(_border);
        HoverBrush = new SolidColorBrush(_hover);
        HoverBorderBrush = new SolidColorBrush(_hoverBorder);
        PressedBrush = new SolidColorBrush(_pressed);
    }
}
