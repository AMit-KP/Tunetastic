using System;
using Microsoft.UI.Xaml.Markup;

namespace Tunetastic.Common.Services;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public sealed class LocExtension : MarkupExtension
{
	public string Key { get; set; } = string.Empty;

	public LocExtension() { }

	public LocExtension(string key)
	{
		Key = key;
	}

	protected override object ProvideValue()
	{
		return LocalizationService.GetString(Key);
	}

	protected override object ProvideValue(IXamlServiceProvider serviceProvider)
	{
		return LocalizationService.GetString(Key);
	}
}

