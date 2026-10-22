using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Documents;

namespace Tunetastic.Common.Services;

/// <summary>
/// Represents a single bullet note in the release notes with an indentation level.
/// </summary>
public sealed record WhatsNewNote(string Text, int IndentLevel = 0);

/// <summary>
/// Service responsible for displaying the What's New dialog to highlight updates.
/// Automatically triggers once after an update and seals fresh installs.
/// Supports markdown links [text](url), nested bullet indentation, and collapsible previous releases.
/// </summary>
public static class WhatsNewService
{
	private static readonly SemaphoreSlim _dialogLock = new(1, 1);
	private static readonly Regex MarkdownLinkRegex = new(@"\[(?<text>[^\]]+)\]\((?<url>https?://[^\)]+)\)", RegexOptions.Compiled);

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

			var releases = await LoadMarkdownReleasesAsync();
			var match = GetNotesForVersion(releases, currentVersion, lastDoneVersion);
			if (match is not null && match.Value.Notes.Count > 0)
			{
				bool shown = await ShowDialogAsync(match.Value.Version, match.Value.Notes, releases);
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
		var releases = await LoadMarkdownReleasesAsync();
		if (releases.Count == 0)
			return;

		var match = currentVersion is not null ? GetNotesForVersion(releases, currentVersion) : null;
		var versionStr = match?.Version ?? currentVersion?.ToString(3) ?? releases[0].Version;
		var notes = match?.Notes ?? releases[0].Notes;

		if (notes is not null && notes.Count > 0)
		{
			await ShowDialogAsync(versionStr, notes, releases);
		}
	}

	/// <summary>
	/// Finds the release notes corresponding to the target version, or the newest applicable version <= currentVersion.
	/// </summary>
	private static (string Version, List<WhatsNewNote> Notes)? GetNotesForVersion(List<(string Version, List<WhatsNewNote> Notes)> releases, Version currentVersion, Version? lastDoneVersion = null)
	{
		// Exact version match
		foreach (var (ver, list) in releases)
		{
			var parsed = ParseVersion(ver);
			if (parsed is not null && parsed.CompareTo(currentVersion) == 0 && list.Count > 0)
				return (ver.TrimStart('v', 'V'), list);
		}

		// Skipped releases / patch releases: find newest version in changelog <= currentVersion (and > lastDoneVersion if set)
		var candidate = releases
			.Select(r => (Parsed: ParseVersion(r.Version), r.Version, r.Notes))
			.Where(x => x.Parsed is not null && x.Parsed.CompareTo(currentVersion) <= 0 && (lastDoneVersion is null || x.Parsed.CompareTo(lastDoneVersion) > 0) && x.Notes.Count > 0)
			.OrderByDescending(x => x.Parsed)
			.FirstOrDefault();

		if (candidate.Parsed is not null)
			return (candidate.Version.TrimStart('v', 'V'), candidate.Notes);

		return null;
	}

	/// <summary>
	/// Loads and parses the release notes from the packaged markdown asset, preserving release order and indentation levels.
	/// </summary>
	private static async Task<List<(string Version, List<WhatsNewNote> Notes)>> LoadMarkdownReleasesAsync()
	{
		var list = new List<(string Version, List<WhatsNewNote> Notes)>();
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
			List<WhatsNewNote>? currentNotes = null;

			foreach (var rawLine in lines)
			{
				if (string.IsNullOrWhiteSpace(rawLine))
					continue;

				var trimmedStart = rawLine.TrimStart();
				if (trimmedStart.StartsWith("## "))
				{
					if (currentVersion is not null && currentNotes is not null)
					{
						list.Add((currentVersion, currentNotes));
					}

					currentVersion = trimmedStart.Substring(3).Trim();
					currentNotes = [];
				}
				else if (currentNotes is not null && (trimmedStart.StartsWith("- ") || trimmedStart.StartsWith("* ")))
				{
					int spaces = 0;
					foreach (char c in rawLine)
					{
						if (c == ' ') spaces++;
						else if (c == '\t') spaces += 4;
						else break;
					}

					int indentLevel = spaces switch
					{
						<= 1 => 0,
						<= 5 => 1,
						<= 9 => 2,
						_ => 3
					};

					var content = trimmedStart.Substring(2).Trim();
					if (!string.IsNullOrEmpty(content))
					{
						currentNotes.Add(new WhatsNewNote(content, indentLevel));
					}
				}
			}

			if (currentVersion is not null && currentNotes is not null)
			{
				list.Add((currentVersion, currentNotes));
			}
		}
		catch
		{
			// Fall back gracefully if the asset cannot be read
		}

		return list;
	}

	/// <summary>
	/// Builds and displays the ContentDialog modal on the UI thread with links and collapsible previous releases.
	/// </summary>
	private static async Task<bool> ShowDialogAsync(string targetVersion, List<WhatsNewNote> targetNotes, List<(string Version, List<WhatsNewNote> Notes)> allReleases)
	{
		if (!await _dialogLock.WaitAsync(0))
			return false;

		try
		{
			var mainStack = new StackPanel { Spacing = 14, Margin = new Thickness(8, 8, 8, 8) };

			// 1. Current / Target Release Notes
			var currentNotesStack = BuildNotesList(targetNotes);
			mainStack.Children.Add(currentNotesStack);

			// 2. Previous Releases (if any)
			var olderReleases = allReleases
				.Where(r => !string.Equals(r.Version.TrimStart('v', 'V'), targetVersion.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (olderReleases.Count > 0)
			{
				var olderStack = new StackPanel { Spacing = 14, Margin = new Thickness(4) };

				foreach (var (ver, notes) in olderReleases)
				{
					var releaseBlock = new StackPanel { Spacing = 6 };
					releaseBlock.Children.Add(new TextBlock
					{
						Text = $"v{ver.TrimStart('v', 'V')}",
						Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
						Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
					});
					releaseBlock.Children.Add(BuildNotesList(notes));
					olderStack.Children.Add(releaseBlock);
				}

				var expander = new Expander
				{
					Header = "Previous Releases",
					HorizontalAlignment = HorizontalAlignment.Stretch,
					HorizontalContentAlignment = HorizontalAlignment.Stretch,
					IsExpanded = false,
					Content = olderStack
				};

				mainStack.Children.Add(expander);
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
				Text = $"What's New in v{targetVersion.TrimStart('v', 'V')}",
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
					MaxHeight = 420,
					Content = mainStack
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
	/// Builds a StackPanel of bulleted items with support for markdown links [text](url) and nested indentation levels.
	/// </summary>
	private static StackPanel BuildNotesList(List<WhatsNewNote> notes)
	{
		var stack = new StackPanel { Spacing = 6 };
		foreach (var note in notes)
		{
			var itemGrid = new Grid { ColumnSpacing = 8 };
			itemGrid.Margin = new Thickness(note.IndentLevel * 20, 0, 0, 0);
			itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			string bulletChar;
			double bulletSize;
			Microsoft.UI.Xaml.Media.Brush bulletBrush;
			var fontWeight = Microsoft.UI.Text.FontWeights.Normal;

			if (note.IndentLevel == 0)
			{
				bulletChar = "•";
				bulletSize = 16;
				bulletBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
				fontWeight = Microsoft.UI.Text.FontWeights.Bold;
			}
			else if (note.IndentLevel == 1)
			{
				bulletChar = "•";
				bulletSize = 14;
				bulletBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
			}
			else
			{
				bulletChar = "•";
				bulletSize = 11;
				bulletBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
			}

			var bullet = new TextBlock
			{
				Text = bulletChar,
				FontSize = bulletSize,
				FontWeight = fontWeight,
				Foreground = bulletBrush,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0, note.IndentLevel == 1 ? -1 : 0, 0, 0)
			};
			Grid.SetColumn(bullet, 0);

			var text = CreateFormattedTextBlock(note.Text);
			Grid.SetColumn(text, 1);

			itemGrid.Children.Add(bullet);
			itemGrid.Children.Add(text);
			stack.Children.Add(itemGrid);
		}

		return stack;
	}

	/// <summary>
	/// Parses inline markdown links [text](url) into clickable Hyperlink elements.
	/// </summary>
	private static TextBlock CreateFormattedTextBlock(string rawText)
	{
		var textBlock = new TextBlock
		{
			TextWrapping = TextWrapping.WrapWholeWords,
			VerticalAlignment = VerticalAlignment.Center,
			Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
		};

		var matches = MarkdownLinkRegex.Matches(rawText);
		if (matches.Count == 0)
		{
			textBlock.Text = rawText;
			return textBlock;
		}

		int lastIndex = 0;
		foreach (Match match in matches)
		{
			if (match.Index > lastIndex)
			{
				textBlock.Inlines.Add(new Run { Text = rawText.Substring(lastIndex, match.Index - lastIndex) });
			}

			var linkText = match.Groups["text"].Value;
			var url = match.Groups["url"].Value;

			if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			{
				var hyperlink = new Hyperlink { NavigateUri = uri };
				hyperlink.Inlines.Add(new Run { Text = linkText });
				textBlock.Inlines.Add(hyperlink);
			}
			else
			{
				textBlock.Inlines.Add(new Run { Text = linkText });
			}

			lastIndex = match.Index + match.Length;
		}

		if (lastIndex < rawText.Length)
		{
			textBlock.Inlines.Add(new Run { Text = rawText.Substring(lastIndex) });
		}

		return textBlock;
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

