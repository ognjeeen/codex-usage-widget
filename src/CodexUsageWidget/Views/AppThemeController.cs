using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CodexUsageWidget.Infrastructure.Settings;
using CodexUsageWidget.Views.Controls;
using MediaColor = System.Windows.Media.Color;

namespace CodexUsageWidget.Views;

public sealed class AppThemeController : IDisposable
{
    private readonly System.Windows.Application _application;
    private readonly ThemePreferenceMonitor _themePreferences;
    private readonly AccentPaletteStore _accentPaletteStore;
    private AccentPalette _accentPalette;
    private bool _disposed;

    public AppThemeController(
        System.Windows.Application application,
        ThemePreferenceMonitor themePreferences,
        AccentPaletteStore accentPaletteStore)
    {
        _application = application;
        _themePreferences = themePreferences;
        _accentPaletteStore = accentPaletteStore;
        _accentPalette = accentPaletteStore.Load();
        _themePreferences.EffectiveThemeChanged += ThemePreferencesOnEffectiveThemeChanged;
        _themePreferences.SystemThemeChanged += ThemePreferencesOnSystemThemeChanged;
        ApplyEffectiveTheme(themePreferences.EffectiveTheme);
    }

    public event Action<EffectiveTheme>? EffectiveThemeChanged;

    public event Action<EffectiveTheme>? SystemThemeChanged;

    public ThemePreference Preference => _themePreferences.Preference;

    public AccentPalette AccentPalette => _accentPalette;

    public EffectiveTheme EffectiveTheme => _themePreferences.EffectiveTheme;

    public EffectiveTheme SystemTheme => _themePreferences.SystemTheme;

    public void SetPreference(ThemePreference preference)
    {
        _themePreferences.SetPreference(preference);
    }

    public void SetAccentPalette(AccentPalette palette)
    {
        if (_accentPalette == palette)
        {
            return;
        }

        _accentPalette = palette;
        _accentPaletteStore.Save(palette);
        RunOnUiThread(() => ApplyAccentPalette(EffectiveTheme));
    }

    private void ThemePreferencesOnEffectiveThemeChanged(EffectiveTheme theme) =>
        RunOnUiThread(() => ApplyEffectiveTheme(theme));

    private void ThemePreferencesOnSystemThemeChanged(EffectiveTheme theme) =>
        RunOnUiThread(() => SystemThemeChanged?.Invoke(theme));

    private void RunOnUiThread(Action action)
    {
        if (_application.Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _application.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
    }

    private void ApplyEffectiveTheme(EffectiveTheme effectiveTheme)
    {
        AppThemePalette.Apply(_application.Resources, effectiveTheme);
        ApplyAccentPalette(effectiveTheme);
        EffectiveThemeChanged?.Invoke(effectiveTheme);
    }

    private void ApplyAccentPalette(EffectiveTheme effectiveTheme)
    {
        AppAccentPalette.Apply(_application.Resources, effectiveTheme, _accentPalette);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _themePreferences.EffectiveThemeChanged -= ThemePreferencesOnEffectiveThemeChanged;
        _themePreferences.SystemThemeChanged -= ThemePreferencesOnSystemThemeChanged;
        _themePreferences.Dispose();
    }

    private static class AppThemePalette
    {
        private static readonly IReadOnlyDictionary<string, MediaColor> Dark =
            new Dictionary<string, MediaColor>(StringComparer.Ordinal)
            {
                ["TextPrimary"] = Parse("#F2F2F2"),
                ["TextSecondary"] = Parse("#B5B5B5"),
                ["TextTertiary"] = Parse("#969696"),
                ["WidgetSurfaceBrush"] = Parse("#FA1F1F1F"),
                ["WidgetBorderBrush"] = Parse("#343434"),
                ["DialogSurfaceBrush"] = Parse("#1F1F1F"),
                ["DialogBorderBrush"] = Parse("#424242"),
                ["CardBackground"] = Parse("#292929"),
                ["CardBorder"] = Parse("#343434"),
                ["SubtleSurfaceBrush"] = Parse("#252525"),
                ["CodeSurfaceBrush"] = Parse("#171717"),
                ["DividerBrush"] = Parse("#303030"),
                ["IconForegroundBrush"] = Parse("#A8A8A8"),
                ["ControlBackgroundBrush"] = Parse("#343434"),
                ["ControlBorderBrush"] = Parse("#454545"),
                ["ControlHoverBrush"] = Parse("#414141"),
                ["ControlPressedBrush"] = Parse("#292929"),
                ["IconHoverBrush"] = Parse("#2C2C2C"),
                ["IconPressedBrush"] = Parse("#363636"),
                ["ProgressTrackBrush"] = Parse("#3A3A3A"),
                ["UsageNormalBrush"] = Parse("#E7E7E7"),
                ["ScrollThumbBrush"] = Parse("#646464"),
                ["ScrollThumbHoverBrush"] = Parse("#858585"),
                ["TooltipBackgroundBrush"] = Parse("#F5232323"),
                ["TooltipBorderBrush"] = Parse("#4A4A4A"),
                ["DangerForegroundBrush"] = Parse("#F0B3B8"),
                ["NoticeBackgroundBrush"] = Parse("#24DDA56D"),
                ["NoticeForegroundBrush"] = Parse("#E6B47D"),
                ["MenuBackgroundBrush"] = Parse("#242424"),
                ["MenuBorderBrush"] = Parse("#454545"),
                ["MenuHoverBrush"] = Parse("#363636"),
                ["MenuSeparatorBrush"] = Parse("#3B3B3B"),
                ["MenuCheckBorderBrush"] = Parse("#565656"),
                ["MenuCheckForegroundBrush"] = Parse("#FFFFFF")
            };

        private static readonly IReadOnlyDictionary<string, MediaColor> Light =
            new Dictionary<string, MediaColor>(StringComparer.Ordinal)
            {
                ["TextPrimary"] = Parse("#1D1D1F"),
                ["TextSecondary"] = Parse("#55565A"),
                ["TextTertiary"] = Parse("#74767B"),
                ["WidgetSurfaceBrush"] = Parse("#FCFFFFFF"),
                ["WidgetBorderBrush"] = Parse("#D7D9DE"),
                ["DialogSurfaceBrush"] = Parse("#FAFAFB"),
                ["DialogBorderBrush"] = Parse("#D7D9DE"),
                ["CardBackground"] = Parse("#F2F3F5"),
                ["CardBorder"] = Parse("#E1E3E7"),
                ["SubtleSurfaceBrush"] = Parse("#F4F5F7"),
                ["CodeSurfaceBrush"] = Parse("#F0F1F3"),
                ["DividerBrush"] = Parse("#E1E3E7"),
                ["IconForegroundBrush"] = Parse("#66686D"),
                ["ControlBackgroundBrush"] = Parse("#FFFFFF"),
                ["ControlBorderBrush"] = Parse("#D3D5DA"),
                ["ControlHoverBrush"] = Parse("#F0F2F5"),
                ["ControlPressedBrush"] = Parse("#E4E6EA"),
                ["IconHoverBrush"] = Parse("#ECEEF1"),
                ["IconPressedBrush"] = Parse("#E0E2E6"),
                ["ProgressTrackBrush"] = Parse("#DADDE2"),
                ["UsageNormalBrush"] = Parse("#3B78D8"),
                ["ScrollThumbBrush"] = Parse("#B1B4BA"),
                ["ScrollThumbHoverBrush"] = Parse("#8C9097"),
                ["TooltipBackgroundBrush"] = Parse("#FCFFFFFF"),
                ["TooltipBorderBrush"] = Parse("#CED1D6"),
                ["DangerForegroundBrush"] = Parse("#B4232B"),
                ["NoticeBackgroundBrush"] = Parse("#1AF0A24A"),
                ["NoticeForegroundBrush"] = Parse("#9A5B17"),
                ["MenuBackgroundBrush"] = Parse("#FAFAFB"),
                ["MenuBorderBrush"] = Parse("#D7D9DE"),
                ["MenuHoverBrush"] = Parse("#ECEEF1"),
                ["MenuSeparatorBrush"] = Parse("#E1E3E7"),
                ["MenuCheckBorderBrush"] = Parse("#B8BBC1"),
                ["MenuCheckForegroundBrush"] = Parse("#FFFFFF")
            };

        public static void Apply(ResourceDictionary resources, EffectiveTheme theme)
        {
            foreach (var (key, color) in theme == EffectiveTheme.Light ? Light : Dark)
            {
                resources[key] = new SolidColorBrush(color);
            }
        }

        private static MediaColor Parse(string value) =>
            (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(value);
    }

    private static class AppAccentPalette
    {
        public static void Apply(
            ResourceDictionary resources,
            EffectiveTheme theme,
            AccentPalette palette)
        {
            var colors = Resolve(theme, palette);
            resources["AccentPrimaryColor"] = colors.Primary;
            resources["AccentPrimaryBorderColor"] = colors.Border;
            resources["AccentPrimaryHoverColor"] = colors.Hover;
            resources["AccentPrimaryHoverBorderColor"] = colors.HoverBorder;
            resources["AccentPrimaryPressedColor"] = colors.Pressed;
            resources["AccentDataColor"] = colors.Data;
            SetBrushColor(resources, "AccentPrimaryBrush", colors.Primary);
            SetBrushColor(resources, "AccentPrimaryBorderBrush", colors.Border);
            SetBrushColor(resources, "AccentDataBrush", colors.Data);
            SetBrushColor(resources, "UsageNormalBrush", colors.Data);

            AccentButton.ApplyPalette(
                colors.Primary,
                colors.Border,
                colors.Hover,
                colors.HoverBorder,
                colors.Pressed);
        }

        private static void SetBrushColor(
            ResourceDictionary resources,
            string key,
            MediaColor color)
        {
            if (resources[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
                return;
            }

            resources[key] = new SolidColorBrush(color);
        }

        private static AccentColors Resolve(EffectiveTheme theme, AccentPalette palette) =>
            theme == EffectiveTheme.Light
                ? ResolveLight(palette)
                : ResolveDark(palette);

        private static AccentColors ResolveLight(AccentPalette palette) => palette switch
        {
            AccentPalette.Violet => Colors("#7C3AED", "#6D28D9", "#6D28D9", "#5B21B6", "#5B21B6", "#7C3AED"),
            AccentPalette.Teal => Colors("#0F766E", "#0D6B64", "#0D6B64", "#0B5D57", "#0B5D57", "#0F766E"),
            AccentPalette.Emerald => Colors("#15803D", "#137337", "#137337", "#116530", "#116530", "#15803D"),
            AccentPalette.Pink => Colors("#BE185D", "#A91552", "#A91552", "#941047", "#941047", "#BE185D"),
            _ => Colors("#3B78D8", "#326BC3", "#326BC3", "#2859A6", "#2859A6", "#3B78D8")
        };

        private static AccentColors ResolveDark(AccentPalette palette) => palette switch
        {
            AccentPalette.Violet => Colors("#7C3AED", "#9565E8", "#8B5CF6", "#A78BFA", "#6D28D9", "#A78BFA"),
            AccentPalette.Teal => Colors("#0F8F83", "#32B8AA", "#14A89A", "#5EEAD4", "#0D766C", "#5EEAD4"),
            AccentPalette.Emerald => Colors("#1D9148", "#42B968", "#26A957", "#6EE7B7", "#18783C", "#6EE7B7"),
            AccentPalette.Pink => Colors("#C72F75", "#E25998", "#D9468A", "#F9A8D4", "#A92363", "#F9A8D4"),
            _ => Colors("#3B78D8", "#5B91E5", "#4A88E8", "#76ADF2", "#2E63B5", "#75A7F0")
        };

        private static AccentColors Colors(
            string primary,
            string border,
            string hover,
            string hoverBorder,
            string pressed,
            string data) =>
            new(
                Parse(primary),
                Parse(border),
                Parse(hover),
                Parse(hoverBorder),
                Parse(pressed),
                Parse(data));

        private static MediaColor Parse(string value) =>
            (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(value);

        private sealed record AccentColors(
            MediaColor Primary,
            MediaColor Border,
            MediaColor Hover,
            MediaColor HoverBorder,
            MediaColor Pressed,
            MediaColor Data);
    }
}
