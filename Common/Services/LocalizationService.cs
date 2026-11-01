using Microsoft.Windows.ApplicationModel.Resources;

namespace Tunetastic.Common.Services;

/// <summary>
/// Provides application-wide localization and string resource lookup services.
/// </summary>
public static class LocalizationService
{
	private static ResourceLoader? _resourceLoader;
	private static readonly object _lock = new();

	private static ResourceLoader Loader
	{
		get
		{
			if (_resourceLoader == null)
			{
				lock (_lock)
				{
					_resourceLoader ??= new ResourceLoader();
				}
			}
			return _resourceLoader;
		}
	}

	/// <summary>
	/// Retrieves the localized string corresponding to the specified resource key.
	/// If the key cannot be found or is empty, returns the key itself as a fallback.
	/// </summary>
	/// <param name="resourceKey">The key identifier in the resource dictionary.</param>
	/// <returns>The localized string, or the key itself if not found.</returns>
	public static string GetString(string resourceKey)
	{
		if (string.IsNullOrEmpty(resourceKey))
		{
			return string.Empty;
		}

		try
		{
			string value = Loader.GetString(resourceKey);
			return string.IsNullOrEmpty(value) ? resourceKey : value;
		}
		catch
		{
			return resourceKey;
		}
	}

	/// <summary>
	/// Re-initializes the resource loader when the language or resource context changes at runtime.
	/// </summary>
	public static void Refresh()
	{
		lock (_lock)
		{
			_resourceLoader = new ResourceLoader();
		}
	}
}

