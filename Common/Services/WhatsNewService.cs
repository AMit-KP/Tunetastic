namespace Tunetastic.Common.Services;

/// <summary>
/// Service responsible for displaying the What's New dialog to highlight updates.
/// Automatically triggers once after an update and seals fresh installs.
/// </summary>
public static class WhatsNewService
{
	private static readonly SemaphoreSlim _dialogLock = new(1, 1);

	/// <summary>
	/// Checks if the app was updated to a newer version and displays the What's New dialog once.
	/// On a fresh install, the current version is sealed without displaying the dialog.
	/// Existing installs updating from older versions (e.g. v1.66 -> v1.80) are recognized
	/// and presented with the What's New dialog.
	/// </summary>
	public static void ShowIfEligible()
	{
		App.MainWindow.DispatcherQueue.TryEnqueue(async () =>
		{
			// Give MainPage layout a brief moment to finish loading and rendering
			await Task.Delay(500);

			var currentVersion = ParseVersion(ProcessInfoHelper.GetFileVersionInfo().FileVersion);
			if (currentVersion is null)
				return;

			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			var lastDoneVersionText = localSettings.Values[nameof(LocalSave.WhatsNewDoneVersion)]?.ToString();
			var lastDoneVersion = ParseVersion(lastDoneVersionText);

			// Fresh install vs existing user check:
			// If WhatsNewDoneVersion has never been recorded, check whether existing user data exists
			// (such as songs in the database or saved user preferences).
			if (lastDoneVersionText is null)
			{
				var songsCount = await DatabaseHelper.Instance.TryGetSongsCount();
				bool hasExistingData = (songsCount is not null && songsCount.Value > 0)
					|| localSettings.Values.ContainsKey(nameof(LocalSave.Theme))
					|| localSettings.Values.ContainsKey(nameof(LocalSave.Backdrop))
					|| localSettings.Values.ContainsKey(nameof(LocalSave.AutoScanEnabled));

				if (!hasExistingData)
				{
					// True fresh install: seal current version immediately so dialog only shows on subsequent updates
					localSettings.Values[nameof(LocalSave.WhatsNewDoneVersion)] = currentVersion.ToString(3);
					return;
				}

				// Otherwise, user is updating from an older version (e.g. v1.66) where WhatsNew was not yet present.
				// Treat them as updating to currentVersion so they get to see the What's New dialog!
			}
			else if (lastDoneVersion is not null && currentVersion.CompareTo(lastDoneVersion) <= 0)
			{
				// Already shown for this version or a newer one
				return;
			}

			var changelog = await LoadMarkdownChangelogAsync();
			var match = GetNotesForVersion(changelog, currentVersion, lastDoneVersion);
			if (match is not null && match.Value.Notes.Count > 0)
			{
				bool shown = await ShowDialogAsync(match.Value.Version, match.Value.Notes);
				if (shown)
				{
					localSettings.Values[nameof(LocalSave.WhatsNewDoneVersion)] = currentVersion.ToString(3);
				}
			}
			else
			{
				localSettings.Values[nameof(LocalSave.WhatsNewDoneVersion)] = currentVersion.ToString(3);
			}
		});
	}

	/// <summary>
	/// Displays the What's New dialog on demand (e.g. from Settings > About).
	/// </summary>
	public static async Task ShowManualAsync()
	{
		var currentVersion = ParseVersion(ProcessInfoHelper.GetFileVersionInfo().FileVersion);
		var changelog = await LoadMarkdownChangelogAsync();
		if (changelog.Count == 0)
			return;

		var match = currentVersion is not null ? GetNotesForVersion(changelog, currentVersion) : null;
		var versionStr = match?.Version ?? currentVersion?.ToString(3) ?? changelog.Keys.FirstOrDefault() ?? string.Empty;
		var notes = match?.Notes ?? changelog.Values.FirstOrDefault();

		if (notes is not null && notes.Count > 0)
		{
			await ShowDialogAsync(versionStr, notes);
		}
	}

	/// <summary>
	/// Finds the release notes corresponding to the target version, or the newest applicable version <= currentVersion.
	/// </summary>
	private static (string Version, List<string> Notes)? GetNotesForVersion(Dictionary<string, List<string>> changelog, Version currentVersion, Version? lastDoneVersion = null)
	{
		// Exact version match
		foreach (var (key, list) in changelog)
		{
			var parsed = ParseVersion(key);
			if (parsed is not null && parsed.CompareTo(currentVersion) == 0 && list.Count > 0)
				return (key.TrimStart('v', 'V'), list);
		}

		// Skipped releases / patch releases: find newest version in changelog <= currentVersion (and > lastDoneVersion if set)
		var candidate = changelog
			.Select(kvp => (Parsed: ParseVersion(kvp.Key), Key: kvp.Key, Notes: kvp.Value))
			.Where(x => x.Parsed is not null && x.Parsed.CompareTo(currentVersion) <= 0 && (lastDoneVersion is null || x.Parsed.CompareTo(lastDoneVersion) > 0) && x.Notes.Count > 0)
			.OrderByDescending(x => x.Parsed)
			.FirstOrDefault();

		if (candidate.Parsed is not null)
			return (candidate.Key.TrimStart('v', 'V'), candidate.Notes);

		return null;
	}

	/// <summary>
	/// Loads and parses the release notes from the packaged markdown asset.
	/// </summary>
	private static async Task<Dictionary<string, List<string>>> LoadMarkdownChangelogAsync()
	{
		var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string filePath = Path.Combine(AppContext.BaseDirectory, "Assets", "WhatsNew", "whatsnew.md");
			string markdown;
			if (File.Exists(filePath))
			{
				markdown = await File.ReadAllTextAsync(filePath);
			}
			else
			{
				var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/WhatsNew/whatsnew.md"));
				markdown = await Windows.Storage.FileIO.ReadTextAsync(file);
			}

			var lines = markdown.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
			string? currentVersion = null;

			foreach (var rawLine in lines)
			{
				var line = rawLine.Trim();
				if (line.StartsWith("## "))
				{
					currentVersion = line.Substring(3).Trim();
					if (!dict.ContainsKey(currentVersion))
						dict[currentVersion] = [];
				}
				else if (currentVersion is not null && (line.StartsWith("- ") || line.StartsWith("* ")))
				{
					var content = line.Substring(2).Trim();
					if (!string.IsNullOrEmpty(content))
						dict[currentVersion].Add(content);
				}
			}
		}
		catch
		{
			// Fall back gracefully if the asset cannot be read
		}

		return dict;
	}

	/// <summary>
	/// Builds and displays the ContentDialog modal on the UI thread.
	/// </summary>
	private static async Task<bool> ShowDialogAsync(string version, List<string> notes)
	{
		if (!await _dialogLock.WaitAsync(0))
			return false;

		try
		{
			var contentStack = new StackPanel { Spacing = 10, Margin = new Thickness(0, 8, 0, 0), Width = 460 };
			foreach (var note in notes)
			{
				var itemGrid = new Grid { ColumnSpacing = 10 };
				itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

				var bullet = new TextBlock
				{
					Text = "•",
					FontSize = 16,
					FontWeight = Microsoft.UI.Text.FontWeights.Bold,
					Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
					VerticalAlignment = VerticalAlignment.Top
				};
				Grid.SetColumn(bullet, 0);

				var text = new TextBlock
				{
					Text = note,
					TextWrapping = TextWrapping.WrapWholeWords,
					VerticalAlignment = VerticalAlignment.Center,
					Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
				};
				Grid.SetColumn(text, 1);

				itemGrid.Children.Add(bullet);
				itemGrid.Children.Add(text);
				contentStack.Children.Add(itemGrid);
			}

			var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
			titlePanel.Children.Add(new Image
			{
				Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/AppIcon.png")),
				Width = 24,
				Height = 24
			});
			titlePanel.Children.Add(new TextBlock
			{
				Text = $"What's New in v{version}",
				Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
				VerticalAlignment = VerticalAlignment.Center
			});

			var dialog = new ContentDialog
			{
				Title = titlePanel,
				RequestedTheme = App.Current.ThemeService.ActualTheme,
				CloseButtonText = "Close",
				DefaultButton = ContentDialogButton.Close,
				Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AcrylicBackgroundFillColorBaseBrush"],
				Content = new ScrollViewer
				{
					MaxHeight = 350,
					Content = contentStack
				},
				XamlRoot = App.MainWindow.Content.XamlRoot
			};

			MainWindow._instance.WindowResizePermission(false);
			await dialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);
			return true;
		}
		catch
		{
			return false;
		}
		finally
		{
			_dialogLock.Release();
		}
	}

	/// <summary>
	/// Parses a version string into a comparable 3-part <see cref="Version"/>.
	/// </summary>
	private static Version? ParseVersion(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return null;

		text = text.Trim().TrimStart('v', 'V');
		var numbers = text.Split('.').Take(3).ToList();
		while (numbers.Count < 3)
			numbers.Add("0");

		return Version.TryParse(string.Join('.', numbers), out var version) ? version : null;
	}
}

