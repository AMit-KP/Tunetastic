using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Tunetastic.Generated.Protos;

namespace Tunetastic.Views.LibraryViews;

/// <summary>
/// Represents a page in the application that displays a collection of all available songs.
/// </summary>
/// <remarks>
/// This class is a part of the library view system in the application and is designed to work with the application's song data.
/// It initializes a collection of songs upon construction and provides functionality to load the song collection as a playlist for playback.
/// </remarks>
public sealed partial class AllSongsViewPage : Page
{
	/// <summary>
	/// Gets or sets the collection of all songs available in the application.
	/// </summary>
	/// <remarks>
	/// The <c>AllSongs</c> property holds an observable collection of <c>Song</c> objects, representing the full list
	/// of songs loaded from the application's data source. This property is primarily used to populate the user interface
	/// and manage interactions with the song list.
	/// The collection is initialized and populated when the page instance is created. This property is also bound to
	/// the <c>ListView</c> in the associated XAML to display the songs in the UI, allowing users to interact with
	/// individual items.
	/// </remarks>
	public ObservableCollection<Song> AllSongs
	{
		get; set;
	} = new();

	private readonly DispatcherQueue _dispatcherQueue;

	/// <summary>
	/// Represents a page for displaying and managing all available songs in the application.
	/// </summary>
	/// <remarks>
	/// This class is responsible for initializing and displaying a collection of songs. It provides functionalities
	/// such as managing the song list and integrating it as a playlist for playback. The page's content is dynamically
	/// updated through asynchronous operations.
	/// </remarks>
	public AllSongsViewPage()
	{
		this.InitializeComponent();
		_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		_ = CheckScanning();
	}

	/// <summary>
	/// Asynchronously checks whether the application is currently scanning for music data and handles UI updates accordingly.
	/// </summary>
	/// <remarks>
	/// This method monitors the scanning process managed by the music data service. It updates the user interface elements,
	/// such as progress indicators, during the scanning operation and reloads the content when scanning completes.
	/// If no scanning is in progress, it initializes the song collection by loading metadata from a binary data file
	/// and applies sorting to the collection.
	/// </remarks>
	/// <returns>
	/// A task that represents the asynchronous operation for checking and handling music data scanning and subsequent UI updates.
	/// </returns>
	private async Task CheckScanning()
	{
		if (GetMusicDataService.IsScanning)
		{
			LoadingProgress.Opacity = 0;
			LoadingProgress.Visibility = Visibility.Visible;
			PageButtons.Visibility = Visibility.Collapsed;
			for (double i = 0; i <= 1; i += 0.05)
			{
				LoadingProgress.Opacity = i;
				await Task.Delay(1);
			}
			while (GetMusicDataService.IsScanning)
			{
				ProgressFill.Width = GetMusicDataService.ScanProgress * 4;
				ProgressFillText.Text = $"{GetMusicDataService.ScanProgress.ToString()}%";
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
				this.Content = new AllSongsViewPage();
			});
			return;
		}
		AllSongs.AddRange(ProtobufData.LoadFromBin<SongList>(DataFile.AllSongsMetaData).Songs);
		UpdateSorting();
	}

	/// <summary>
	/// Updates the sorting preferences for the song list displayed on the AllSongsViewPage.
	/// </summary>
	/// <remarks>
	/// This method determines the sorting criteria and order (e.g., by title, artist, album, duration.)
	/// based on the user's saved preferences in local settings. It also updates the selection status
	/// of the UI elements corresponding to the sorting options and triggers the list update.
	/// </remarks>
	private void UpdateSorting()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var sortBy = localSettings.Values[nameof(LocalSave.AllSongViewSortBy)]?.ToString() ?? "Title";
		var sortOrder = localSettings.Values[nameof(LocalSave.AllSongViewSortOrder)]?.ToString() ?? "Ascending";
		switch (sortBy)
		{
			case "Title":
				Title.IsChecked = true;
				break;
			case "Artists":
				Artists.IsChecked = true;
				break;
			case "Album":
				Album.IsChecked = true;
				break;
			case "Duration":
				Duration.IsChecked = true;
				break;
			default:
				Title.IsChecked = true;
				break;
		}
		switch (sortOrder)
		{
			case "Ascending":
				Ascending.IsChecked = true;
				break;
			case "Descending":
				Descending.IsChecked = true;
				break;
			default:
				Ascending.IsChecked = true;
				break;
		}
		UpdateListBasedOnSorting();
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
	private void UpdateListBasedOnSorting()
	{
		var selectedSong = AllSongsListView.SelectedItem;
		var sortBy = Sort.Items.OfType<RadioMenuFlyoutItem>()
						 .Where(item => item.GroupName == "SortBy" && item.IsChecked).Select(item => item.Text)
						 .FirstOrDefault() ??
					 "Title";
		var orderBy =
			Sort.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "Order" && item.IsChecked)
				.Select(item => item.Text).FirstOrDefault() ?? "Ascending";
		bool order = orderBy == "Ascending";

		List<Song> newList = new();
		IOrderedEnumerable<string>? availableLetters = null;
		bool hasSpecialCharacters = false;
		switch (sortBy)
		{
			case "Title":
				newList = order ? AllSongs.OrderBy(s => s.Title).ToList() : AllSongs.OrderByDescending(s => s.Title).ToList();
				availableLetters = AllSongs.Select(song => song.Title.Substring(0, 1).ToUpper()).Distinct().OrderBy(c => c);
				hasSpecialCharacters = (AllSongs.Select(song => song.Title.Substring(0, 1)).Where(c => !char.IsLetter(c[0])).Distinct().OrderBy(c => c).ToList()).Any();

				break;
			case "Artists":
				newList = order ? AllSongs.OrderBy(s => s.Artists).ToList() : AllSongs.OrderByDescending(s => s.Artists).ToList();
				availableLetters = AllSongs.Select(song => song.Artists.Substring(0, 1).ToUpper()).Distinct().OrderBy(c => c);
				hasSpecialCharacters = (AllSongs.Select(song => song.Artists.Substring(0, 1)).Where(c => !char.IsLetter(c[0])).Distinct().OrderBy(c => c).ToList()).Any();
				break;
			case "Album":
				newList = order ? AllSongs.OrderBy(s => s.Album).ToList() : AllSongs.OrderByDescending(s => s.Album).ToList();
				availableLetters = AllSongs.Select(song => song.Album.Substring(0, 1).ToUpper()).Distinct().OrderBy(c => c);
				hasSpecialCharacters = (AllSongs.Select(song => song.Album.Substring(0, 1)).Where(c => !char.IsLetter(c[0])).Distinct().OrderBy(c => c).ToList()).Any();
				break;
			case "Duration":
				newList = order ? AllSongs.OrderBy(s => s.Duration).ToList() : AllSongs.OrderByDescending(s => s.Duration).ToList();
				break;
		}
		AllSongs.Clear();
		AllSongs.AddRange(newList);
		SortDropDown.Content = $"Sort By: {sortBy} {(order ? "⬆️" : "⬇️")}";
		ToolTipService.SetToolTip(SortDropDown, $"The list is sorted by {sortBy} column in {orderBy} order.");
		AllSongsListView.SelectedItem = selectedSong;
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.AllSongViewSortBy)] = sortBy;
		localSettings.Values[nameof(LocalSave.AllSongViewSortOrder)] = orderBy;

		PopulateAlphabetNavigation(availableLetters, order, sortBy, hasSpecialCharacters);
	}


	/// <summary>
	/// Handles the ItemClick event for the ListView control in the AllSongsViewPage.
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
		List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CurrentPlaylist)] = "AllSongsViewPage";
		MusicPlayer.Instance.LoadPlaylist(songPaths, track?.Path);
	}

	/// <summary>
	/// Loads the collection of all available songs as a playlist and starts preparing them for playback.
	/// </summary>
	/// <remarks>
	/// This method retrieves the file paths of all songs in the collection and initializes a playlist within the music player.
	/// It ensures that the songs are ready for playback and begins with the specified track as the currently active item.
	/// </remarks>
	public void LoadAsPlayList()
	{
		List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
		MusicPlayer.Instance.LoadLastPlayed(songPaths);
	}

	/// <summary>
	/// Scrolls to a specific song in the `AllSongsListView`.
	/// </summary>
	/// <param name="song">The song object to scroll to. If null, no action is performed.</param>
	/// <returns>A task representing the asynchronous operation of scrolling to the specified song.</returns>
	private async Task ScrollToSong(Song? song)
	{
		if (song != null)
		{
			await AllSongsListView.SmoothScrollIntoViewWithItemAsync(song, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
			AllSongsListView.SelectedItem = song;
		}

	}

	/// <summary>
	/// Handles the Loaded event for the AllSongsViewPage.
	/// </summary>
	/// <param name="sender">The source of the event, typically the page itself.</param>
	/// <param name="e">The event data associated with the Loaded event.</param>
	/// <remarks>
	/// This method verifies if the current playlist corresponds to "AllSongsViewPage" by accessing the application's
	/// local settings. If the last played song is found in the local settings, it attempts to scroll to that song
	/// within the songs list. The scrolling operation is performed asynchronously with a slight delay.
	/// </remarks>
	private void Page_Loaded(object sender, RoutedEventArgs e)
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		if (localSettings.Values[nameof(LocalSave.CurrentPlaylist)]?.ToString() == "AllSongsViewPage")
		{
			var SelectedSong = AllSongs.Select(s => s).Where(s => s.Path == localSettings.Values[nameof(LocalSave.LastPlayedTrack)]?.ToString()).FirstOrDefault();
			_ = ScrollToSong(SelectedSong);
		}
	}

	/// <summary>
	/// Handles the Sort button click event to update the song list based on the selected sorting criteria.
	/// </summary>
	/// <param name="sender">The control that triggered the event, typically a UI element like a menu flyout item.</param>
	/// <param name="e">Event data associated with the Sort button click.</param>
	private void SortButton_OnClick(object sender, RoutedEventArgs e)
	{
		UpdateListBasedOnSorting();
		AdjustAlphabetSize();
		//TODO resort current playlist
	}

	private void ViewButton_OnClick(object sender, RoutedEventArgs e)
	{
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
		List<string> songPaths = AllSongs.Select(s => s.Path).ToList();

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlaylist)] = "AllSongsViewPage";

		var startingSong = songPaths[new Random().Next(songPaths.Count)];
		MusicPlayer.Instance.LoadPlaylist(songPaths, startingSong);
		var SelectedSong = AllSongs.Select(s => s).Where(s => s.Path == startingSong).FirstOrDefault();
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
		List<string> songPaths = AllSongs.Select(s => s.Path).ToList();
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.CurrentPlaylist)] = "AllSongsViewPage";
		MusicPlayer.Instance.LoadPlaylist(songPaths);
		await ScrollToSong(AllSongs[0]);
		ShuffleAndPlay.IsEnabled = true;
	}

	/// <summary>
	/// Populates the alphabet navigation panel with letters and optionally a special character marker for sections of songs.
	/// </summary>
	/// <param name="availableLetters">
	/// A collection of available letters representing sections that contain songs. If null, all letters are displayed as unavailable.
	/// </param>
	/// <param name="order">
	/// A boolean value indicating whether the letters should be displayed in ascending or descending order.
	/// </param>
	/// <param name="sortBy">
	/// Specifies the sorting criteria used for navigating to a letter section within the song collection.
	/// </param>
	/// <param name="hasSpecialCharacters">
	/// A boolean value indicating whether special characters (e.g., "#", "1", "2"...) should be included in the navigation panel.
	/// </param>
	/// <remarks>
	/// Clears the existing children of the alphabet navigation panel before dynamically generating and adding new letter elements. Each letter is styled
	/// and configured based on its availability within the provided letter collection. Interactive behaviors such as tapping and pointer events are implemented
	/// for letters available for navigation.
	/// </remarks>
	private void PopulateAlphabetNavigation(IOrderedEnumerable<string>? availableLetters, bool order, string sortBy, bool hasSpecialCharacters)
	{
		AlphabetNavigationPanel.Children.Clear();

		var fullAlphabet = Enumerable.Range('A', 26).Select(x => ((char)x).ToString());
		if (hasSpecialCharacters) fullAlphabet = fullAlphabet.Reverse().Append("#").Reverse();
		if (!order) fullAlphabet = fullAlphabet.Reverse();

		foreach (var letter in fullAlphabet)
		{
			bool hasSongs = availableLetters == null ? false : (availableLetters.Contains(letter)) || (letter == "#" && hasSpecialCharacters);

			var textElement = new TextBlock
			{
				Text = letter,
				Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
				Foreground = new SolidColorBrush(hasSongs ? Colors.White : Colors.Gray),
				Opacity = hasSongs ? 1 : 0.5,
				IsHitTestVisible = hasSongs,
				Margin = new Thickness(0, 2, 0, 2),
				TextAlignment = TextAlignment.Right
			};

			if (hasSongs)
			{
				textElement.Tapped += (s, e) => ScrollToSection(letter, sortBy);

				textElement.PointerEntered += (s, e) =>
				((TextBlock)s).FontSize *= 1.5;

				textElement.PointerExited += (s, e) =>
					((TextBlock)s).FontSize *= 0.666;
			}

			AlphabetNavigationPanel.Children.Add(textElement);
		}
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
				targetSong = letter != "#" ? AllSongs.FirstOrDefault(song => song.Title.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) : AllSongs.FirstOrDefault(song => !char.IsLetter(song.Title[0]));
				break;

			case "Artists":
				targetSong = letter != "#" ? AllSongs.FirstOrDefault(song => song.Artists.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) : AllSongs.FirstOrDefault(song => !char.IsLetter(song.Artists[0]));
				break;

			case "Album":
				targetSong = letter != "#" ? AllSongs.FirstOrDefault(song => song.Album.StartsWith(letter, StringComparison.OrdinalIgnoreCase)) : AllSongs.FirstOrDefault(song => !char.IsLetter(song.Album[0]));
				break;
		}
		if (targetSong != null)
		{
			await AllSongsListView.SmoothScrollIntoViewWithItemAsync(targetSong, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false, additionalVerticalOffset: -40);
			AllSongsListView.SelectedItem = targetSong;
			await Task.Delay(500);
			await AllSongsListView.SmoothScrollIntoViewWithItemAsync(targetSong, itemPlacement: ScrollItemPlacement.Top, disableAnimation: false, scrollIfVisible: false, additionalVerticalOffset: -40);
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
		double availableSpace = AllSongsListView.ActualHeight - 40;
		double totalLetters = AlphabetNavigationPanel.Children.Count;

		// 🔹 Auto-fit button height dynamically
		double autoHeight = availableSpace / totalLetters;

		foreach (var textElement in AlphabetNavigationPanel.Children.OfType<TextBlock>())
		{
			textElement.Height = autoHeight;
			textElement.Margin = new Thickness(0);
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
		AlphabetNavigationPanel.Children.OfType<TextBlock>().Where(textElement => textElement.Opacity == 1).ToList().ForEach(textElement => textElement.Foreground = themeBrush);
	}
}
