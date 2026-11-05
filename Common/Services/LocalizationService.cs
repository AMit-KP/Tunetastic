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

		// 1. Direct lookup
		try
		{
			string value = Loader.GetString(resourceKey);
			if (!string.IsNullOrEmpty(value))
			{
				return value;
			}
		}
		catch { }

		// 2. Attached ToolTipService migration fallback
		if (resourceKey.Contains(".ToolTipService.ToolTip"))
		{
			try
			{
				string ttKey = resourceKey.Replace(".ToolTipService.ToolTip", "_ToolTip");
				string value = Loader.GetString(ttKey);
				if (!string.IsNullOrEmpty(value))
				{
					return value;
				}
			}
			catch { }
		}

		// 3. Dot to slash path resolution (WinUI 3 MRT Core hierarchical paths)
		if (resourceKey.Contains('.'))
		{
			try
			{
				string slashKey = resourceKey.Replace('.', '/');
				string value = Loader.GetString(slashKey);
				if (!string.IsNullOrEmpty(value))
				{
					return value;
				}
			}
			catch { }
		}

		// 4. Common property suffixes fallback (/Title, /Text, /Content, _ToolTip)
		string[] suffixes = { "/Title", "/Text", "/Content", "_ToolTip" };
		foreach (var suffix in suffixes)
		{
			try
			{
				string value = Loader.GetString(resourceKey + suffix);
				if (!string.IsNullOrEmpty(value))
				{
					return value;
				}
			}
			catch { }
		}

		// 5. Strip common property suffix if key was requested with .Text / .Content / .Title
		string[] stripSuffixes = { ".Text", ".Content", ".Title" };
		foreach (var s in stripSuffixes)
		{
			if (resourceKey.EndsWith(s, StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					string baseKey = resourceKey.Substring(0, resourceKey.Length - s.Length);
					string value = Loader.GetString(baseKey);
					if (!string.IsNullOrEmpty(value))
					{
						return value;
					}
				}
				catch { }
			}
		}

		return resourceKey;
	}

	/// <summary>
	/// Selects either the singular or plural resource key template based on the given count.
	/// e.g. "Notification_Queue_Added" with count=1 returns "Notification_Queue_Added_Singular",
	/// while count != 1 returns "Notification_Queue_Added_Plural".
	/// </summary>
	private static string GetPluralString(string baseKey, int count)
	{
		string key = count == 1 ? $"{baseKey}_Singular" : $"{baseKey}_Plural";
		return GetString(key);
	}

	/// <summary>
	/// Selects the appropriate singular or plural template and formats it with count as {0},
	/// plus any additional positional arguments as {1}, {2}, etc.
	/// </summary>
	public static string FormatPluralString(string baseKey, int count, params object[] extraArgs)
	{
		string template = GetPluralString(baseKey, count);
		if (extraArgs == null || extraArgs.Length == 0)
		{
			return string.Format(template, count);
		}
		var allArgs = new object[extraArgs.Length + 1];
		allArgs[0] = count;
		Array.Copy(extraArgs, 0, allArgs, 1, extraArgs.Length);
		return string.Format(template, allArgs);
	}

	/// <summary>
	/// Retrieves a localized template string and formats it with the provided arguments.
	/// </summary>
	public static string FormatString(string key, params object[] args)
	{
		string template = GetString(key);
		return string.Format(template, args);
	}

	/// <summary>
	/// Formats the confirmation message displayed when deleting a single song from the library or playlist.
	/// </summary>
	public static string FormatDeleteSingleSongMessage(string? title, string? artists, string? album, string? path)
	{
		return $"{GetString("DeleteDialog_SingleSong_Question")}\n\n" +
		       $"{GetString("DeleteDialog_Detail_Title")} {title ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_Artist")} {artists ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_Album")} {album ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_File")} {path ?? string.Empty}";
	}

	/// <summary>
	/// Formats the success notification displayed after a song has been deleted.
	/// </summary>
	public static string FormatSongDeletedNotification(string? title, string? artists, string? album, string? path)
	{
		return $"{GetString("Notification_SongDeleted_Title")}\n" +
		       $"{GetString("DeleteDialog_Detail_Title")} {title ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_Artist")} {artists ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_Album")} {album ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_File")} {path ?? string.Empty}";
	}

	/// <summary>
	/// Formats the notification displayed after a song has been removed from a playlist.
	/// </summary>
	public static string FormatSongRemovedFromPlaylistNotification(string playlistName, string? title, string? artists, string? album, string? path)
	{
		return $"{FormatString("Notification_SongRemovedFromPlaylist_Title", playlistName)}\n" +
		       $"{GetString("DeleteDialog_Detail_Title")} {title ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_Artist")} {artists ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_Album")} {album ?? string.Empty}\n" +
		       $"{GetString("DeleteDialog_Detail_File")} {path ?? string.Empty}";
	}

	/// <summary>
	/// Returns a randomly chosen empty-library humorous quote with Line 1 and Line 2 localized.
	/// </summary>
	public static (string Line1, string Line2) GetRandomEmptyLibraryQuote()
	{
		int index = System.Random.Shared.Next(1, 16); // 1 to 15 inclusive
		string line1 = GetString($"EmptyState_Quote_{index}_Line1");
		string line2 = GetString($"EmptyState_Quote_{index}_Line2");
		return (line1, line2);
	}

	public static void Refresh()
	{
		lock (_lock)
		{
			_resourceLoader = new ResourceLoader();
		}
	}
}
