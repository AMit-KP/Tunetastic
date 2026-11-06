using System;
using Microsoft.UI.Xaml.Markup;

namespace Tunetastic.Common.Services;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class LocExtension : MarkupExtension
{
	public string Key { get; set; } = string.Empty;
	public bool? AsField { get; set; }

	public LocExtension() { }

	public LocExtension(string key)
	{
		Key = key;
	}

	private string Resolve()
	{
		bool isField = AsField ?? LocalizationService.IsFieldKey(Key);
		return isField ? LocalizationService.FormatFieldLabel(Key) : LocalizationService.GetString(Key);
	}

	protected override object ProvideValue()
	{
		return Resolve();
	}

	protected override object ProvideValue(IXamlServiceProvider serviceProvider)
	{
		return Resolve();
	}
}

