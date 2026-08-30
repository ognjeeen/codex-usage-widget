using System.Windows.Markup;
using CodexUsageWidget.Localization;

namespace CodexUsageWidget.Views.Localization;

[MarkupExtensionReturnType(typeof(object))]
public sealed class LocExtension(string key) : MarkupExtension
{
    public string Key { get; } = key;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new System.Windows.Data.Binding($"[{Key}]")
        {
            Mode = System.Windows.Data.BindingMode.OneWay,
            Source = Strings.Current
        }.ProvideValue(serviceProvider);
}
