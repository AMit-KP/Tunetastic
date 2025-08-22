using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Represents the detail page for a specific year in the music library.
/// </summary>
/// <remarks>
/// This page is used within the application to display songs grouped by year. It provides
/// functionalities such as handling visual transitions during navigation, managing the year group
/// header, and interacting with the song collection related to a specific year.
/// </remarks>
public sealed partial class YearDetailPage : Page
{
	/// <summary>
	/// Gets or sets the collection of songs associated with a specific year grouping.
	/// </summary>
	public ObservableCollection<Song> YearGroupSongs
	{
		get; set;
	} = new();

	private Song? selectedSong;
	private readonly DispatcherQueue _dispatcherQueue;

	public YearDetailPage()
	{
		this.InitializeComponent();
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		_ = CheckScanning();
	}

	/// <summary>
	/// Called when the page is navigated to. Handles the setup of the year group header and its associated animation.
	/// </summary>
	/// <param name="e">Navigation event arguments containing the year parameter passed during navigation</param>
	/// <remarks>
	/// This override performs three main tasks:
	/// <br/>
	/// 1. Sets the year group header text based on the navigation parameter
	/// <br/>
	/// 2. Applies a connected animation to the year header for smooth visual transitions
	/// <br/>
	/// 3. Triggers the year selection in the navigation
	/// <br/>
	/// <br/>
	/// The year parameter is expected to be passed during navigation, where "Unknown" is handled as a special case
	/// and displayed as "Unknown Year".
	/// </remarks>
	protected override async void OnNavigatedTo(NavigationEventArgs e)
	{
		ActualYearGroup.Text = (e.Parameter.ToString() == "Unknown" ? "Unknown Year" : e.Parameter.ToString());
		var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("YearHeaderAnimation");

		if (animation != null)
		{
			await ActualYearGroup.DispatcherQueue.EnqueueAsync(() =>
			{
				animation.TryStart(ActualYearGroup);
			});
		}

		SelectYearOnNavigation();
	}

	/// <summary>
	/// Handles the navigation animation and visibility when navigating away from the current page.
	/// </summary>
	/// <param name="e">Navigation event arguments containing information about the navigation.</param>
	/// <remarks>
	/// This method performs two main tasks:
	/// <br/>
	/// 1. Prepares a connected animation for the year header to maintain visual continuity
	/// <br/>
	/// 2. Handles the visibility transition of the ActualYearGroup element:
	/// <br/>
	///    - When navigating to YearsViewPage: Applies a fade-out animation
	/// <br/>
	///    - For other destinations: Immediately collapses the element
	/// </remarks>
	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		if (e.SourcePageType.Name == "YearsViewPage")
		{
			ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("YearHeaderAnimationBack", ActualYearGroup);

			var fadeOut = new DoubleAnimation
			{
				To = 0,
				Duration = TimeSpan.FromMilliseconds(30),
				FillBehavior = FillBehavior.Stop
			};
			Storyboard.SetTarget(fadeOut, ActualYearGroup);
			Storyboard.SetTargetProperty(fadeOut, "Opacity");

			var sb = new Storyboard();
			sb.Children.Add(fadeOut);
			sb.Completed += (_, __) => ActualYearGroup.Visibility = Visibility.Collapsed;
			sb.Begin();
		}
	}

	/// <summary>
	/// Asynchronously checks the status of the ongoing music data scanning process and interacts with the user interface based on the status.
	/// </summary>
	/// <remarks>
	/// This method verifies whether a music scanning operation is in progress through the data service. If scanning is active,
	/// it modifies the application's UI by displaying loading indicators and hiding specific content elements until the process completes.
	/// Once scanning finishes or no scanning is detected, it populates the song collection with metadata, applies sorting and view style updates,
	/// and adjusts the visibility of various UI components.
	/// </remarks>
	/// <returns>
	/// A task representing the asynchronous operation of scanning status monitoring, UI adjustments, and song collection management.
	/// </returns>
	private async Task CheckScanning()
	{
		GoToSettings.Visibility = Visibility.Visible;
		YearDetailListViewGrid.Visibility = Visibility.Collapsed;
		YearDetailCompactViewGrid.Visibility = Visibility.Collapsed;
		PageButtons.Visibility = Visibility.Collapsed;

		if (GetMusicData.IsScanning)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			LoadingProgress.Opacity = 0;
			LoadingProgress.Visibility = Visibility.Visible;

			for (double i = 0; i <= 1; i += 0.05)
			{
				LoadingProgress.Opacity = i;
				await Task.Delay(1);
			}

			while (GetMusicData.IsScanning)
			{
				ProgressFill.Width = GetMusicData.ScanProgress * 4;
				ProgressFillText.Text = $"{GetMusicData.ScanProgress.ToString()}%";
				await Task.Delay(1);
			}

			for (double i = 1; i >= 0; i -= 0.05)
			{
				LoadingProgress.Opacity = i;
				await Task.Delay(1);
			}
			LoadingProgress.Visibility = Visibility.Collapsed;
			await _dispatcherQueue.EnqueueAsync(() =>
			{
				this.Content = new YearDetailPage();
			});
			return;
		}

		if (await DatabaseHelper.Instance.GetSongsCount() > 0)
		{
			GoToSettings.Visibility = Visibility.Collapsed;
			ViewButton.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Visible;
			UpdateAsPerLastViewStyle();
			UpdateAsPerLastSorting();
		}
	}

	/// <summary>
	/// Updates the sorting preferences for the song list displayed on the YearDetailViewPage.
	/// </summary>
	/// <remarks>
	/// This method determines the sorting criteria and order (e.g., by title, artist, album, duration.)
	/// based on the user's saved preferences in local settings. It also updates the selection status
	/// of the UI elements corresponding to the sorting options and triggers the list update.
	/// </remarks>
	private void UpdateAsPerLastSorting()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortBy = localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)]?.ToString() ?? "Title";
		var sortOrder = localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)]?.ToString() ?? "Ascending";
		switch (sortBy)
		{
			case "Artists":
				Artists.IsChecked = true;
				break;

			case "Album":
				Album.IsChecked = true;
				break;

			case "Duration":
				Duration.IsChecked = true;
				break;

			case "Title":
			default:
				Title.IsChecked = true;
				break;
		}
		switch (sortOrder)
		{
			case "Descending":
				Descending.IsChecked = true;
				break;

			case "Ascending":
			default:
				Ascending.IsChecked = true;
				break;
		}
		_ = UpdateListBasedOnSorting();
	}

	/// <summary>
	/// Updates the current view style of the song collection display using the last saved preference.
	/// </summary>
	/// <remarks>
	/// This method retrieves the previously saved view style setting from the application's local settings and
	/// applies it to the song collection display. Supported view styles include "List View", "Compact View", and "Card View".
	/// If no preference is found, the default view style is set to "Compact View".
	/// </remarks>
	private void UpdateAsPerLastViewStyle()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var viewStyle = localSettings.Values[nameof(LocalSave.YearDetailViewStyle)]?.ToString() ?? "Compact View";
		switch (viewStyle)
		{
			case "List View":
				ListViewStyle.IsChecked = true;
				break;

			case "Compact View":
			default:
				CompactViewStyle.IsChecked = true;
				break;
		}
		_ = UpdateListBasedOnViewStyle();
	}

	/// <summary>
	/// Updates the UI elements and layout to match the current view style selected for displaying songs.
	/// </summary>
	/// <remarks>
	/// This method dynamically adjusts the visibility of UI components depending on the selected view style,
	/// such as "List View" or "Compact View". It also persists the user's selection in local settings for future sessions.
	/// Additionally, it attempts to scroll to a previously selected song after applying the view style changes.
	/// </remarks>
	/// <returns>
	/// A Task representing the asynchronous operation of updating the view style and scrolling to a specific song.
	/// </returns>
	private async Task UpdateListBasedOnViewStyle()
	{
		var viewStyle = ViewStyle.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "View" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Compact View";
		string? glyph = null;
		switch (viewStyle)
		{
			case "List View":
				YearDetailListViewGrid.Visibility = Visibility.Visible;
				YearDetailCompactViewGrid.Visibility = Visibility.Collapsed;
				glyph = "\uE8FD";
				break;

			case "Compact View":
			default:
				YearDetailListViewGrid.Visibility = Visibility.Collapsed;
				YearDetailCompactViewGrid.Visibility = Visibility.Visible;
				glyph = "\uE71D";
				break;
		}
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.YearDetailViewStyle)] = viewStyle;
		ViewButton.Content = new FontIcon() { Glyph = glyph };
		ToolTipService.SetToolTip(ViewButton, viewStyle);
		await ScrollToSong(selectedSong);       //somehow this doesn't work
		await Task.Delay(500);
		await ScrollToSong(selectedSong);
	}

	/// <summary>
	/// Updates the list of songs based on the selected sorting criteria and order.
	/// </summary>
	/// <remarks>
	/// This method processes the current sorting preferences, such as the column to sort by (e.g., Title, Artists, Album, or Duration)
	/// and the order (Ascending or Descending). It modifies the displayed song list accordingly and ensures the current selection remains intact.
	/// Additional functionality includes updating the user interface with the sorting details and storing the preferences
	/// in local application settings for persistence. The alphabet navigation is also refreshed with relevant data based on the sorting criteria.
	/// </remarks>
	private async Task UpdateListBasedOnSorting()
	{
		var song = selectedSong;
		var sortBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "SortBy" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Title";
		var orderBy = Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool AscOrder = orderBy == "Ascending";

		IOrderedEnumerable<string>? availableLetters = null;
		bool hasSpecialCharacters = false;

		var newList = await DatabaseHelper.Instance.LoadSongsFromDB(orderBy: Enum.Parse<SongProperty>(sortBy), ascending: AscOrder, whereCondition: $"{SongProperty.Year.ToString()} = '{ActualYearGroup.Text}'");
		YearGroupSongs.Clear();
		YearGroupSongs.AddRange(newList);
		newList = null;

		switch (sortBy)
		{
			case "Title":
				availableLetters = YearGroupSongs.Select(song => song.Title.Substring(0, 1).ToUpper()).Distinct().OrderBy(c => c);
				hasSpecialCharacters = YearGroupSongs.Select(song => song.Title.Substring(0, 1)).Where(c => !char.IsLetter(c[0])).Distinct().OrderBy(c => c).ToList().Any();
				break;
			case "Artists":
				availableLetters = YearGroupSongs.Select(song => song.Artists.Substring(0, 1).ToUpper()).Distinct().OrderBy(c => c);
				hasSpecialCharacters = YearGroupSongs.Select(song => song.Artists.Substring(0, 1)).Where(c => !char.IsLetter(c[0])).Distinct().OrderBy(c => c).ToList().Any();
				break;
			case "Album":
				availableLetters = YearGroupSongs.Select(song => song.Album.Substring(0, 1).ToUpper()).Distinct().OrderBy(c => c);
				hasSpecialCharacters = YearGroupSongs.Select(song => song.Album.Substring(0, 1)).Where(c => !char.IsLetter(c[0])).Distinct().OrderBy(c => c).ToList().Any();
				break;
		}

		var sortDropdownContent = new TextBlock();
		sortDropdownContent.Inlines.Add(new Run { Text = "Sort By: " });
		sortDropdownContent.Inlines.Add(new Run { Text = sortBy, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		sortDropdownContent.Inlines.Add(new Run { Text = $" {(AscOrder ? "⬆️" : "⬇️")}" });

		var orderDropdownTooltip = new TextBlock();
		orderDropdownTooltip.Inlines.Add(new Run { Text = "The list is sorted by " });
		orderDropdownTooltip.Inlines.Add(new Run { Text = sortBy, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		orderDropdownTooltip.Inlines.Add(new Run { Text = $" column in " });
		orderDropdownTooltip.Inlines.Add(new Run { Text = orderBy, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
		orderDropdownTooltip.Inlines.Add(new Run { Text = " order." });

		SortDropDown.Content = sortDropdownContent;
		ToolTipService.SetToolTip(SortDropDown, orderDropdownTooltip);

		song = YearGroupSongs.Select(s => s).Where(s => s.Path == song?.Path).FirstOrDefault();
		await ScrollToSong(song);       //somehow this doesn't work
		await Task.Delay(1000);
		await ScrollToSong(song);
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.YearDetailViewSortBy)] = sortBy;
		localSettings.Values[nameof(LocalSave.YearDetailViewSortOrder)] = orderBy;

		PopulateAlphabetNavigation(availableLetters, AscOrder, sortBy, hasSpecialCharacters);
	}

	/// <summary>
	/// Determines the currently selected view style for the song collection display.
	/// </summary>
	/// <remarks>
	/// This method identifies the active view style based on the selection in the view style menu
	/// and returns the corresponding ListView instance. The supported view styles include
	/// "List View" and "Compact View", with "Compact View" being the default.
	/// </remarks>
	/// <returns>
	/// The <see cref="ListView"/> instance corresponding to the currently selected view style.
	/// </returns>
	private ListView GetCurrentViewStyle()
	{
		var viewStyle = ViewStyle.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "View" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Compact View";
		return viewStyle switch
		{
			"List View" => YearDetailListView,
			"Compact View" => YearDetailCompactView,
			_ => YearDetailCompactView
		};
	}

	/// <summary>
	/// Handles the ItemClick event for the ListView control in the YearDetailViewPage.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ListView control.</param>
	/// <param name="e">Provides data for the ItemClick event, including the clicked item.</param>
	/// <remarks>
	/// This method is triggered when a user clicks an item in the song list. It retrieves the clicked song,
	/// generates a playlist from the current collection of songs, and loads the clicked song into the music player for playback.
	/// The playlist is also saved as the current playlist in the application's local settings.
	/// </remarks>
	private void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var track = e.ClickedItem as Song;
		List<string> songPaths = YearGroupSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, track?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"YearGroup>{ActualYearGroup.Text}";
	}

	/// <summary>
	/// Scrolls to a specific song in the View.
	/// </summary>
	/// <param name="song">The song object to scroll to. If null, no action is performed.</param>
	/// <returns>A task representing the asynchronous operation of scrolling to the specified song.</returns>
	private async Task ScrollToSong(Song? song)
	{
		var listView = GetCurrentViewStyle();
		if (song != null)
		{
			await listView.SmoothScrollIntoViewWithItemAsync(song, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
			listView.SelectedItem = song;
		}
	}

	/// <summary>
	/// Handles the Loaded event for the YearDetailViewPage.
	/// </summary>
	/// <param name="sender">The source of the event, typically the page itself.</param>
	/// <param name="e">The event data associated with the Loaded event.</param>
	/// <remarks>
	/// This method is responsible for managing the initialization operations required when the page is loaded. It checks whether the current playlist corresponds to the "YearDetailViewPage" and retrieves the last played song from the application's local settings, if available. It then attempts to scroll to the position of the last played song in the song collection asynchronously with a minor delay.
	/// </remarks>
	private async void Page_Loaded(object sender, RoutedEventArgs e)
	{
		while (YearGroupSongs == null || YearGroupSongs.Count == 0)
		{
			await Task.Delay(100);
		}
		ScrollToCurrentPlayingTrack();
	}

	/// <summary>
	/// Scrolls the view to the currently playing track if the current playlist corresponds to the "YearDetailViewPage".
	/// </summary>
	/// <remarks>
	/// This method checks the local application settings to determine if the "YearDetailViewPage" is the active playlist.
	/// If it is, the method retrieves the last played track based on its path from the saved settings and attempts to scroll
	/// the page to that specific song within the song collection.
	/// </remarks>
	private void ScrollToCurrentPlayingTrack()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var currentPlaylist = localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? "";
		if (currentPlaylist.StartsWith("YearGroup>") && currentPlaylist.Substring("YearGroup>".Length) == ActualYearGroup.Text)
		{
			var SelectedSong = YearGroupSongs.Select(s => s).Where(s => s.Path == localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString()).FirstOrDefault();
			_ = ScrollToSong(SelectedSong);
		}
	}

	/// <summary>
	/// Handles the Sort button click event to update the song list based on the selected sorting criteria.
	/// </summary>
	/// <param name="sender">The control that triggered the event, typically a UI element like a menu flyout item.</param>
	/// <param name="e">Event data associated with the Sort button click.</param>
	private async void SortButton_OnClick(object sender, RoutedEventArgs e)
	{
		await UpdateListBasedOnSorting();
		await AdjustAlphabetSize();
		var currentPlaylist = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() ?? "";
		if (currentPlaylist.StartsWith("YearGroup>") && currentPlaylist.Substring("YearGroup>".Length) == ActualYearGroup.Text)
		{
			List<string> songPaths = YearGroupSongs.Select(s => s.Path).ToList();
			MusicPlayer.Instance.LoadPlaylist(songPaths, MusicPlayer.Instance.CurrentSong, MusicPlayer.Instance.MediaPlayer.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing, dontReloadCurrent: true);
		}
	}

	/// <summary>
	/// Handles the click event triggered by a view style button in the UI, updating the display style
	/// of the song collection based on the selected view style.
	/// </summary>
	/// <param name="sender">The source of the event, typically the view style button that was clicked.</param>
	/// <param name="e">The event data associated with the click event.</param>
	/// <remarks>
	/// This method is responsible for determining the selected view style (e.g., List View, Compact View, Card View),
	/// updating the visibility of UI elements accordingly, and persisting the selected view style for future use.
	/// It also performs additional UI adjustments such as resizing the alphabet display.
	/// </remarks>
	private async void ViewButton_OnClick(object sender, RoutedEventArgs e)
	{
		await UpdateListBasedOnViewStyle();
		await AdjustAlphabetSize();
	}

	/// <summary>
	/// Handles the click event of the "Shuffle and Play" button to shuffle the song list
	/// and begin playback from a randomly selected song.
	/// </summary>
	/// <param name="sender">The source of the click event, typically the "Shuffle and Play" button.</param>
	/// <param name="e">Provides data about the click event.</param>
	/// <remarks>
	/// This method disables the button to prevent repeated triggers, enables shuffle mode on the music player,
	/// and retrieves the list of song paths to shuffle and load as a playlist. It then randomly selects a starting song
	/// from the playlist and scrolls to that song in the user interface. After a brief delay, it ensures that the song
	/// is properly scrolled into view and re-enables the button.
	/// </remarks>
	private async void ShuffleAndPlayButton_OnClick(object sender, RoutedEventArgs e)
	{
		ShuffleAndPlay.IsEnabled = false;
		MusicPlayer.Instance.ToggleShuffle(ShuffleMode.On);
		List<string> songPaths = YearGroupSongs.Select(s => s.Path).ToList();

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"YearGroup>{ActualYearGroup.Text}";

		var startingSong = songPaths[new Random().Next(songPaths.Count)];
		MusicPlayer.Instance.LoadPlaylist(songPaths, startingSong);
		var SelectedSong = YearGroupSongs.Select(s => s).Where(s => s.Path == startingSong).FirstOrDefault();
		await ScrollToSong(SelectedSong);       //somehow this doesn't work
		await Task.Delay(500);
		await ScrollToSong(SelectedSong);
		ShuffleAndPlay.IsEnabled = true;
	}

	/// <summary>
	/// Handles the click event for the "Play All" button and initiates playback of all songs in the current view.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Play All" button.</param>
	/// <param name="e">Provides data for the routed event that triggered the method.</param>
	/// <remarks>
	/// This method disables shuffle mode, creates a playlist from all songs in the current view,
	/// stores the name of the current playlist in application settings, and starts playing the songs in order.
	/// It also scrolls to the first song in the playlist after initiating playback.
	/// </remarks>
	private async void PlayAllButton_OnClick(object sender, RoutedEventArgs e)
	{
		ShuffleAndPlay.IsEnabled = false;
		MusicPlayer.Instance.ToggleShuffle(ShuffleMode.Off);
		List<string> songPaths = YearGroupSongs.Select(s => s.Path).ToList();
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"YearGroup>{ActualYearGroup.Text}";
		MusicPlayer.Instance.LoadPlaylist(songPaths);
		await ScrollToSong(YearGroupSongs[0]);
		ShuffleAndPlay.IsEnabled = true;
	}

	/// <summary>
	/// Populates the alphabet navigation panel with letters and optionally a special character marker
	/// for navigating sections of songs.
	/// </summary>
	/// <param name="availableLetters">
	/// A collection of letters representing song sections to be included in navigation. Null indicates
	/// all letters are marked as unavailable.
	/// </param>
	/// <param name="order">
	/// A flag indicating whether the letters are ordered in ascending or descending order.
	/// </param>
	/// <param name="sortBy">
	/// The sorting criterion to define navigation to specific column in the song collection.
	/// </param>
	/// <param name="hasSpecialCharacters">
	/// A flag specifying whether special characters (e.g., "#") are included in the navigation.
	/// </param>
	/// <remarks>
	/// This method clears all existing child elements in the alphabet navigation panel before creating
	/// and adding dynamically generated navigation elements. Each letter element is configured based on
	/// its validity from the provided collection. Additionally, it defines interaction behavior for
	/// navigable elements to handle user input.
	/// </remarks>
	private async void PopulateAlphabetNavigation(IOrderedEnumerable<string>? availableLetters, bool order, string sortBy, bool hasSpecialCharacters)
	{
		AlphabetNavigationPanel.Children.Clear();
		if (availableLetters == null && !hasSpecialCharacters) return;

		var fullAlphabet = Enumerable.Range('A', 26).Select(x => ((char)x).ToString());
		if (hasSpecialCharacters) fullAlphabet = fullAlphabet.Reverse().Append("#").Reverse();
		if (!order) fullAlphabet = fullAlphabet.Reverse();

		var viewStyle = ViewStyle.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "View" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Compact View";
		double availableSpace = ContentGrid.ActualHeight - viewStyle switch
		{
			"List View" => 50,
			"Compact View" => 10,
			_ => 10
		};
		if (availableSpace <= 0) return;

		double autoHeight = availableSpace / fullAlphabet.Count();

		foreach (var letter in fullAlphabet)
		{
			bool hasSongs = availableLetters == null ? false : (availableLetters.Contains(letter)) || (letter == "#" && hasSpecialCharacters);

			var Button = new Button
			{
				Content = letter,
				Foreground = new SolidColorBrush(hasSongs ? App.Current.ThemeService.GetActualTheme() == ElementTheme.Dark ? Colors.White : Colors.Black : Colors.Gray),
				Opacity = hasSongs ? 1 : 0.5,
				Background = new SolidColorBrush(Colors.Transparent),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0),
				IsHitTestVisible = hasSongs,
				Margin = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Right,
				VerticalContentAlignment = VerticalAlignment.Stretch,
				Padding = new Thickness(0),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Stretch,
				Height = autoHeight
			};

			if (hasSongs)
			{
				ToolTipService.SetPlacement(Button, Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Left);
				ToolTipService.SetToolTip(Button, letter);
				Button.Tapped += (s, e) => ScrollToSection(letter, sortBy);
			}

			AlphabetNavigationPanel.Children.Add(Button);
			await Task.Delay(1);
		}
		_ = AdjustAlphabetSize();
		availableLetters = null;
	}

	/// <summary>
	/// Scrolls the view to a specific section of the song list based on the specified letter and sorting criteria.
	/// </summary>
	/// <param name="letter">The starting letter of the section to scroll to, or "#" to scroll to non-alphabetic entries.</param>
	/// <param name="sortBy">The property by which the song list is currently sorted. Valid values include "Title", "Artists", and "Album".</param>
	/// <remarks>
	/// This method locates the first song in the collection that matches the specified starting letter and sorting property.
	/// If a matching song is found, the view scrolls to bring the song into focus.
	/// </remarks>
	private async void ScrollToSection(string letter, string sortBy)
	{
		Song? targetSong = null;
		switch (sortBy)
		{
			case "Title":
				targetSong = letter != "#" ? YearGroupSongs.FirstOrDefault(song => song.Title.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) : YearGroupSongs.FirstOrDefault(song => !char.IsLetter(song.Title[0]));
				break;

			case "Artists":
				targetSong = letter != "#" ? YearGroupSongs.FirstOrDefault(song => song.Artists.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) : YearGroupSongs.FirstOrDefault(song => !char.IsLetter(song.Artists[0]));
				break;

			case "Album":
				targetSong = letter != "#" ? YearGroupSongs.FirstOrDefault(song => song.Album.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) : YearGroupSongs.FirstOrDefault(song => !char.IsLetter(song.Album[0]));
				break;
		}
		if (targetSong != null)
		{
			var listView = GetCurrentViewStyle();
			await listView.SmoothScrollIntoViewWithItemAsync(targetSong, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false, additionalVerticalOffset: listView == YearDetailListView ? -40 : 0);
			listView.SelectedItem = targetSong;
			await Task.Delay(500);
			await listView.SmoothScrollIntoViewWithItemAsync(targetSong, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false, additionalVerticalOffset: listView == YearDetailListView ? -40 : 0);
		}
	}

	/// <summary>
	/// Adjusts the size of the elements in the alphabet navigation panel based on the available vertical space.
	/// </summary>
	/// <remarks>
	/// This method calculates the height for each element in the alphabet navigation panel dynamically, ensuring
	/// that the elements are evenly spaced and fit within the available vertical space.
	/// </remarks>
	/// <returns>
	/// A task that represents the asynchronous operation of resizing the elements in the alphabet navigation panel.
	/// </returns>
	private Task<Task> AdjustAlphabetSize()
	{
		var viewStyle = ViewStyle.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "View" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Compact View";
		double availableSpace = ContentGrid.ActualHeight - viewStyle switch
		{
			"List View" => 50,
			"Compact View" => 10,
			_ => 10
		};

		AlphabetNavigationPanel.Margin = viewStyle switch
		{
			"List View" => new Thickness(0, 50, 30, 10),
			"Compact View" => new Thickness(0, 10, 30, 10),
			_ => new Thickness(0, 10, 30, 10)
		};

		if (availableSpace <= 0) return Task.FromResult(Task.CompletedTask);

		double totalLetters = AlphabetNavigationPanel.Children.Count;

		double autoHeight = availableSpace / totalLetters;

		foreach (var button in AlphabetNavigationPanel.Children.OfType<Button>())
		{
			button.Height = autoHeight;
		}
		return Task.FromResult(Task.CompletedTask);
	}

	/// <summary>
	/// Handles the event triggered when the page's size changes.
	/// </summary>
	/// <param name="sender">The source of the event, typically the page.</param>
	/// <param name="e">The event data containing information about the new size of the page.</param>
	/// <remarks>
	/// This method adjusts the layout or size of elements on the page whenever the size of the page changes.
	/// It enqueues a task on the dispatcher queue to perform required layout updates asynchronously.
	/// </remarks>
	private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		var pageHeight = e.NewSize.Height;
		_dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
		{
			await AdjustAlphabetSize();
		});
	}

	/// <summary>
	/// Handles the theme change event for the page.
	/// </summary>
	/// <param name="sender">The <see cref="FrameworkElement"/> that triggered the theme change event.</param>
	/// <param name="args">The event data associated with the theme change event.</param>
	/// <remarks>
	/// This method updates the foreground color of visible text elements in the AlphabetNavigationPanel
	/// based on the current theme of the page. If the theme is dark, white color is applied; otherwise, black color is applied.
	/// </remarks>
	private void Page_ActualThemeChanged(FrameworkElement sender, object args)
	{
		Brush themeBrush = (sender.ActualTheme == ElementTheme.Dark) ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
		AlphabetNavigationPanel.Children.OfType<Button>().Where(button => button.Opacity == 1).ToList().ForEach(textElement => textElement.Foreground = themeBrush);
	}

	/// <summary>
	/// Handles the event when the "Go to Settings" button is clicked.
	/// </summary>
	/// <param name="sender">The source of the event, typically the button being clicked.</param>
	/// <param name="e">The event data associated with the button click.</param>
	/// <remarks>
	/// This method navigates the application to the SettingsPage. It utilizes the application's navigation service (IJsonNavigationService)
	/// to redirect the user to the appropriate page.
	/// </remarks>
	private void GotoSettigsButton_Click(object sender, RoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(SettingsPage));
	}

	/// <summary>
	/// Handles the SelectionChanged event for the ListView to modify UI elements or update internal state based on user selection changes.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ListView control where the selection was changed.</param>
	/// <param name="e">Provides data about the SelectionChanged event, including the modified selection.</param>
	/// <remarks>
	/// This method dynamically updates the `MoreButton`'s state depending on the selection mode and selected items count.
	/// If the ListView is in multi-select mode, the button is enabled or disabled based on whether any items are selected.
	/// In single-select mode, updates the `selectedSong` property with the currently selected song.
	/// </remarks>
	private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		if (listView.IsMultiSelectCheckBoxEnabled)
		{
			MoreButton.IsEnabled = listView.SelectedItems.Count > 0;
		}
		else
			selectedSong = listView.SelectedItem as Song;
	}

	/// <summary>
	/// Handles the Unloaded event for the YearDetailViewPage.
	/// </summary>
	/// <remarks>
	/// This method is triggered when the page is unloaded. It performs cleanup operations such as clearing
	/// the song collection, releasing memory resources, and initiating garbage collection.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the page being unloaded.</param>
	/// <param name="e">The event arguments associated with the Unloaded event.</param>
	private void Page_Unloaded(object sender, RoutedEventArgs e)
	{
		YearGroupSongs.Clear();
		YearGroupSongs = null;
		GC.Collect();
	}

	/// <summary>
	/// Handles the 'Opened' event of the <see cref="MenuFlyout"/> control in the context menu.
	/// </summary>
	/// <remarks>
	/// This method dynamically populates the "Add to Playlist" submenu with the available playlists retrieved from the database.
	/// If no playlists exist, a single "No Playlists created" item is added to the submenu with a red text color.
	/// The method ensures that all items in the submenu are cleared before adding new items.
	/// </remarks>
	/// <param name="sender">The source object where the event is triggered.</param>
	/// <param name="e">An object containing event data related to the 'Opened' event.</param>
	private async void MenuFlyout_Opened(object sender, object e)
	{
		var menu = sender as MenuFlyout;
		var addToPlaylist = menu?.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault();

		addToPlaylist?.Items.Clear();

		List<string> playLists = await DatabaseHelper.Instance.GetAllPlaylistNames();

		if (playLists == null || playLists.Count == 0)
		{
			var menuItem = new MenuFlyoutItem
			{
				Text = "No Playlists created",
				Foreground = new SolidColorBrush(Colors.Red)
			};
			addToPlaylist?.Items.Add(menuItem);
			return;
		}

		foreach (var playList in playLists)
		{
			var menuItem = new MenuFlyoutItem
			{
				Text = playList
			};
			ToolTipService.SetToolTip(menuItem, $"Add {(MultiSelectButton.IsChecked == true ? "selected songs/tracks" : "this song/track")} to {playList} playlist");
			menuItem.Click += AddToPlaylist_Click;
			addToPlaylist?.Items.Add(menuItem);
		}
	}

	/// <summary>
	/// Handles the logic for adding songs to a selected playlist from the current view.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu item representing a playlist.</param>
	/// <param name="e">Event data associated with the click action.</param>
	/// <remarks>
	/// This method is triggered when a user selects the "Add to Playlist" option for one or more songs.
	/// It determines the selected songs from the active view style and adds them to the chosen playlist.
	/// Uses asynchronous operations to interact with the database for playlist updates.
	/// </remarks>
	private async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		if (listView.IsMultiSelectCheckBoxEnabled)
		{
			var songs = listView.SelectedItems;
			List<string> songPaths = songs.Select(s => ((Song)s).Path).ToList();

			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null)
				await DatabaseHelper.Instance.AddSongsToPlaylist(playlist, songPaths);

			GlobalNotification.Info($"{songs.Count} songs added to {playlist} playlist.");
		}
		else
		{
			var song = (sender as MenuFlyoutItem)?.DataContext as Song;
			var playlist = (sender as MenuFlyoutItem)?.Text;

			if (playlist != null && song != null)
			{
				await DatabaseHelper.Instance.AddSongToPlaylist(playlist, song.Path);
				GlobalNotification.Info($"{song.Title} added to {playlist} playlist.");
			}
		}
	}

	/// <summary>
	/// Handles the click event for the "Play" menu flyout item in the song list UI.
	/// </summary>
	/// <param name="sender">The source of the event, typically a menu flyout item associated with a song.</param>
	/// <param name="e">Provides event data for the click event.</param>
	/// <remarks>
	/// This method retrieves the selected song's data from the sender control and prepares a playlist with all available songs.
	/// The playlist is then passed to the application's music player for playback, starting with the selected song.
	/// Additionally, this method updates the application's local settings to store the current playlist context.
	/// </remarks>
	private void MenuFlyoutItemPlay_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		List<string> songPaths = YearGroupSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadPlaylist(songPaths, songData?.Path);
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = $"YearGroup>{ActualYearGroup.Text}";
	}

	/// <summary>
	/// Handles the click event for the "Add to queue" menu flyout item.
	/// </summary>
	/// <remarks>
	/// This method retrieves the associated song data from the menu item's data context
	/// and adds the song's file path to the queued playing list asynchronously.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem that was clicked.</param>
	/// <param name="e">The event data associated with the routed event.</param>
	private async void MenuFlyoutItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		List<string> songPaths = new List<string> { songData?.Path ?? "" };
		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"{songData?.Title} added to queue.");
	}

	/// <summary>
	/// Handles the click event for the "Info/Tag" menu flyout item, displaying detailed song information.
	/// </summary>
	/// <param name="sender">The source of the event, typically the MenuFlyoutItem.</param>
	/// <param name="e">The event data associated with the click action.</param>
	/// <remarks>
	/// This method retrieves the song associated with the selected menu item, queries the song data from
	/// the database, and invokes the main page to display the song's detailed information.
	/// </remarks>
	private async void MenuFlyoutItemInfoTag_OnClick(object sender, RoutedEventArgs e)
	{
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;
		if (songData is not null) MainPage._instance.ShowSongInfo(await DatabaseHelper.Instance.GetSongByPath(songData.Path));
	}

	/// <summary>
	/// Handles the delete action when a menu flyout item is clicked, initiating a confirmation dialog and deleting the selected song.
	/// </summary>
	/// <param name="sender">The source of the event, typically a MenuFlyoutItem representing the delete option.</param>
	/// <param name="e">The event data associated with the click event.</param>
	/// <remarks>
	/// This method displays a confirmation dialog to the user with details about the song to be deleted. If the user confirms the
	/// action, it deletes the file from the local file system, removes the song entry from the database, and updates the collection
	/// of displayed songs to reflect the changes.
	/// </remarks>
	private async void MenuFlyoutItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		DeleteDialog.Visibility = Visibility.Visible;
		var songData = (sender as MenuFlyoutItem)?.DataContext as Song;

		if (songData != null)
		{
			DeleteDialogText.Text = $"Are you sure you want to delete this song/track from your system?" +
									$"\nTitle: {songData.Title}" +
									$"\nArtist: {songData.Artists}" +
									$"\nAlbum: {songData.Album}" +
									$"\nFile: {songData.Path}";

			var result = await DeleteDialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				if (File.Exists(songData.Path))
				{
					File.Delete(songData.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(songData.Path);
					YearGroupSongs.Remove(songData);
					MusicPlayer.Instance.HandleAfterDelete();
					GlobalNotification.Info("Song/Track deleted." +
											$"\nTitle: {songData.Title}" +
											$"\nArtist: {songData.Artists}" +
											$"\nAlbum: {songData.Album}" +
											$"\nFile: {songData.Path}");
				}
			}
			if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
			{
				GoToSettings.Visibility = Visibility.Visible;
				PageButtons.Visibility = Visibility.Collapsed;
			}
			if (YearGroupSongs.Count <= 0)
			{
				App.Current.NavService.GoBack();
				MainPage._instance.RemovePageFromHistory(ActualYearGroup.Text == "Unknown Year" ? "Unknown" : ActualYearGroup.Text);
			}
		}
	}

	/// <summary>
	/// Handles the click event for the MultiSelectButton to toggle between multi-select mode
	/// and single-select mode in the all songs page.
	/// </summary>
	/// <param name="sender">The source of the event, typically the MultiSelectButton.</param>
	/// <param name="e">The event data for the RoutedEventArgs associated with the click action.</param>
	/// <remarks>
	/// When the button is toggled, it switches the functionality of the song list view between multi-select
	/// and single-select modes. In multi-select mode, checkboxes are enabled for selecting multiple items,
	/// and certain UI elements such as context menus are disabled to prevent conflicts. In single-select
	/// mode, standard selection behavior is restored, and UI elements such as context menus are re-enabled.
	/// This method also adjusts the visibility and interactivity of related UI components (e.g., MoreButton,
	/// PlayAllButtonStackPanel) based on the current mode.
	/// </remarks>
	private void MultiSelectButton_Click(object sender, RoutedEventArgs e)
	{
		var view = GetCurrentViewStyle();
		if (MultiSelectButton.IsChecked == true)
		{
			MoreButton.Visibility = Visibility.Visible;
			MoreButton.IsEnabled = false;
			PlayAllButtonStackPanel.Visibility = Visibility.Collapsed;
			SortAndViewButtonPanel.Visibility = Visibility.Collapsed;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn off multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Multiple;
			view.IsItemClickEnabled = false;
			view.IsMultiSelectCheckBoxEnabled = true;
			view.IsRightTapEnabled = false;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(view);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = false;
				}
			}
			if (view.Name == "YearDetailListView") Header.Margin = new Thickness(40, 0, 0, 0);
		}
		else
		{
			MoreButton.Visibility = Visibility.Collapsed;
			PlayAllButtonStackPanel.Visibility = Visibility.Visible;
			SortAndViewButtonPanel.Visibility = Visibility.Visible;
			ToolTipService.SetToolTip(MultiSelectButton, "Turn on multi-select mode");
			view.SelectionMode = ListViewSelectionMode.Single;
			view.IsItemClickEnabled = true;
			view.IsMultiSelectCheckBoxEnabled = false;
			view.IsRightTapEnabled = true;
			var ItemGrids = DevWinUI.DependencyObjectEx.FindDescendants(view);

			foreach (var item in ItemGrids)
			{
				if (item is UIElement uiElement)
				{
					uiElement.IsRightTapEnabled = true;
				}
			}
			if (view.Name == "YearDetailListView") Header.Margin = new Thickness(12, 0, 0, 0);
		}
	}

	/// <summary>
	/// Handles the click event for adding multiple selected songs from the menu flyout to the playing queue.
	/// </summary>
	/// <param name="sender">The source of the event, typically the menu flyout item.</param>
	/// <param name="e">The event data associated with the routed event.</param>
	/// <remarks>
	/// This method retrieves the selected items from the current song view style, extracts their file paths,
	/// and asynchronously adds them to the queued playing list using the <see cref="DatabaseHelper"/> instance.
	/// </remarks>
	private async void MenuFlyoutMultiItemAddToQueue_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems;
		List<string> songPaths = songs.Select(s => ((Song)s).Path).ToList();

		await DatabaseHelper.Instance.AddSongsToQueuedPlayingList(songPaths);

		GlobalNotification.Info($"{songPaths.Count} {(songPaths.Count > 1 ? "songs/tracks" : "song/track")} added to the queue.");
	}

	/// <summary>
	/// Handles the click event for the "Delete" menu flyout item, allowing the user to delete selected songs from the system.
	/// </summary>
	/// <remarks>
	/// This method retrieves the currently selected songs, prompts the user for confirmation through a dialog,
	/// and deletes the selected songs from both the file system and the application's database if confirmed.
	/// </remarks>
	/// <param name="sender">The source of the event, typically the menu flyout item.</param>
	/// <param name="e">Provides data for the routed event, including information about the source and state of the event.</param>
	private async void MenuFlyoutMultiItemDelete_OnClick(object sender, RoutedEventArgs e)
	{
		var songs = GetCurrentViewStyle().SelectedItems;

		List<Song> songList = new();
		foreach (var item in songs)
			songList.Add((Song)item);


		DeleteDialog.Visibility = Visibility.Visible;
		DeleteDialogText.Text = $"Are you sure you want to delete {(songList.Count > 1 ? "these" : "this")} {songList.Count} {(songList.Count > 1 ? "songs/tracks" : "song/track")} from your system?";

		var result = await DeleteDialog.ShowAsync();

		if (result == ContentDialogResult.Primary)
		{
			foreach (Song songData in songList)
			{
				if (File.Exists(songData.Path))
				{
					File.Delete(songData.Path);
					await DatabaseHelper.Instance.DeleteSongFromDB(songData.Path);
					YearGroupSongs.Remove(songData);
				}
			}
			MusicPlayer.Instance.HandleAfterDelete();
			GlobalNotification.Info($"{songList.Count} {(songList.Count > 1 ? "songs/tracks" : "song/track")} deleted.");
		}
		if (await DatabaseHelper.Instance.GetSongsCount() <= 0)
		{
			GoToSettings.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Collapsed;
		}
		if (YearGroupSongs.Count <= 0)
		{
			App.Current.NavService.GoBack();
			MainPage._instance.RemovePageFromHistory(ActualYearGroup.Text == "Unknown Year" ? "Unknown" : ActualYearGroup.Text);
		}
	}

	/// <summary>
	/// Handles the tapped event on a year item, initiating navigation to the YearsViewPage and updating selection state.
	/// </summary>
	/// <param name="sender">The source of the tapped event, usually the UI element that was tapped.</param>
	/// <param name="e">Provides data about the Tapped event, including event-specific properties.</param>
	private void Year_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		App.Current.NavService.NavigateTo(typeof(YearsViewPage), "Years", false, new DrillInNavigationTransitionInfo());
		SelectYearOnNavigation();
	}

	/// <summary>
	/// Sets the selection state of the navigation item associated with the YearsViewPage
	/// to ensure it is highlighted within the application's navigation view.
	/// </summary>
	private static void SelectYearOnNavigation()
	{
		var librariesGroup = App.Current.NavService.MenuItems[1] as NavigationViewItem;

		var libraryNavigationItem = librariesGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == $"Tunetastic.Views.LibraryViews.YearsViewPage");
		libraryNavigationItem.IsSelected = true;
	}
}
