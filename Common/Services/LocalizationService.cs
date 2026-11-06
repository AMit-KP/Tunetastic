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

	private static readonly HashSet<string> s_fieldKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		"Tooltip_Title",
		"Tooltip_Artists",
		"Tooltip_Album",
		"Tooltip_Duration",
		"Tooltip_Year",
		"Tooltip_Track",
		"Tooltip_DateAdded",
		"Tooltip_TimeAdded",
		"Tooltip_DatePlayed",
		"Tooltip_TimePlayed",
		"Tooltip_CountOfSongs",
		"Tooltip_TotalDuration",
		"Tooltip_TimesPlayed",
		"SongInfo_PlayedCount",
		"SongInfo_LastPlayed",
		"Sort_SortBy_Label",
		"Sort_Order_Label",
		"MaxLimit_Label",
		"MaxLimit_Tooltip",
		"PlaylistImport_Label_Playlist",
		"PlaylistImport_Label_Tracks",
		"DeleteDialog_Detail_Title",
		"DeleteDialog_Detail_Artist",
		"DeleteDialog_Detail_Album",
		"DeleteDialog_Detail_File",
	};

	/// <summary>
	/// Returns true if the resource key represents a metadata field or label that requires a locale-aware colon/delimiter.
	/// </summary>
	public static bool IsFieldKey(string? key) => !string.IsNullOrEmpty(key) && s_fieldKeys.Contains(key);

	/// <summary>
	/// Returns the BCP-47 language tag of the currently active language (e.g. "en-US", "fr-FR", "ja-JP", "qps-ploc").
	/// </summary>
	public static string CurrentLanguage
	{
		get
		{
			string overrideLang = Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
			if (!string.IsNullOrEmpty(overrideLang))
			{
				return overrideLang;
			}
			var languages = Windows.Globalization.ApplicationLanguages.Languages;
			if (languages != null && languages.Count > 0)
			{
				return languages[0];
			}
			return System.Globalization.CultureInfo.CurrentUICulture.Name;
		}
	}

	/// <summary>
	/// Formats a resolved field label string and its value according to target language typographic rules:
	/// - French (fr): Non-breaking space before colon ("Label : Value")
	/// - CJK (ja, zh): Full-width colon ("Label：Value")
	/// - RTL (ar, he, fa, ur): Directional isolation markers
	/// - Standard (en and others): Standard colon ("Label: Value")
	/// </summary>
	public static string FormatFieldValue(string label, string value)
	{
		string lang = CurrentLanguage;

		if (lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
		{
			return $"{label}\u00A0: {value}";
		}
		if (lang.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return $"{label}：{value}";
		}
		if (lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("he", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("ur", StringComparison.OrdinalIgnoreCase))
		{
			return $"\u2068{label}: {value}\u2069";
		}

		return $"{label}: {value}";
	}

	/// <summary>
	/// Formats a localized field label (by key) and its value according to target language typographic rules.
	/// </summary>
	public static string FormatField(string labelKey, string value)
	{
		string label = GetString(labelKey);
		return FormatFieldValue(label, value);
	}

	/// <summary>
	/// Formats a field label with its delimiter suffix (without trailing value), e.g. for dropdown prefixes or Run inlines:
	/// - French: "Label : "
	/// - CJK: "Label："
	/// - RTL: "\u2068Label:\u2069 "
	/// - Standard: "Label: "
	/// </summary>
	public static string FormatFieldLabel(string labelKey)
	{
		string label = GetString(labelKey);
		string lang = CurrentLanguage;

		if (lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase))
		{
			return $"{label}\u00A0: ";
		}
		if (lang.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return $"{label}：";
		}
		if (lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("he", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("ur", StringComparison.OrdinalIgnoreCase))
		{
			return $"\u2068{label}:\u2069 ";
		}

		return $"{label}: ";
	}

	/// <summary>
	/// Formats a localized key name with its directional glyph symbol in parentheses according to target language typographic rules:
	/// - CJK (ja, zh): Full-width parentheses ("Key（→）")
	/// - RTL (ar, he, fa, ur): Directional isolation markers ("\u2068Key (→)\u2069")
	/// - Standard: "Key (→)"
	/// </summary>
	public static string FormatKeyWithGlyph(string keyNameKey, string glyph)
	{
		string keyName = GetString(keyNameKey);
		string lang = CurrentLanguage;

		if (lang.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return $"{keyName}（{glyph}）";
		}
		if (lang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("he", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("fa", StringComparison.OrdinalIgnoreCase) ||
		    lang.StartsWith("ur", StringComparison.OrdinalIgnoreCase))
		{
			return $"\u2068{keyName} ({glyph})\u2069";
		}

		return $"{keyName} ({glyph})";
	}

	/// <summary>
	/// Formats the confirmation message displayed when deleting a single song from the library or playlist.
	/// </summary>
	public static string FormatDeleteSingleSongMessage(string? title, string? artists, string? album, string? path)
	{
		return $"{GetString("DeleteDialog_SingleSong_Question")}\n\n" +
		       $"{FormatField("DeleteDialog_Detail_Title", title ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_Artist", artists ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_Album", album ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_File", path ?? string.Empty)}";
	}

	/// <summary>
	/// Formats the success notification displayed after a song has been deleted.
	/// </summary>
	public static string FormatSongDeletedNotification(string? title, string? artists, string? album, string? path)
	{
		return $"{GetString("Notification_SongDeleted_Title")}\n" +
		       $"{FormatField("DeleteDialog_Detail_Title", title ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_Artist", artists ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_Album", album ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_File", path ?? string.Empty)}";
	}

	/// <summary>
	/// Formats the notification displayed after a song has been removed from a playlist.
	/// </summary>
	public static string FormatSongRemovedFromPlaylistNotification(string playlistName, string? title, string? artists, string? album, string? path)
	{
		return $"{FormatString("Notification_SongRemovedFromPlaylist_Title", playlistName)}\n" +
		       $"{FormatField("DeleteDialog_Detail_Title", title ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_Artist", artists ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_Album", album ?? string.Empty)}\n" +
		       $"{FormatField("DeleteDialog_Detail_File", path ?? string.Empty)}";
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
