using CommunityToolkit.WinUI;

namespace Tunetastic.Views.Common;

/// <summary>
/// Shared base for song-list pages built on <see cref="TunetasticPageBase"/> that offer Play All / Shuffle &amp; Play,
/// a compact/list view toggle, A-Z navigation and the shared "Add to playlist" context menu.
/// </summary>
public abstract partial class SongListPageBase : TunetasticPageBase
{
	/// <summary>The view model owning this page's song collection, selection and playback commands.</summary>
	protected SongListViewModel ViewModel { get; } = new();

	/// <summary>
	/// The string written to <see cref="LocalSave.CurrentPlayinglist"/> when this page starts playback.
	/// </summary>
	protected abstract string PlaylistKey { get; }

	/// <summary>
	/// Currently selected song in single-select mode.
	/// </summary>
	protected Song? selectedSong
	{
		get => ViewModel.SelectedSong;
		set => ViewModel.SelectedSong = value;
	}

	/// <summary>
	/// Returns whichever of the two view-style ListViews is active.
	/// </summary>
	protected abstract ListView GetCurrentViewStyle();

	/// <summary>
	/// The button used for shuffle-and-play state.
	/// </summary>
	protected abstract Button ShuffleAndPlayControl { get; }

	/// <summary>
	/// The button whose IsEnabled reflects multi-select state.
	/// </summary>
	protected abstract Button MoreButtonControl { get; }

	private MenuFlyout? _viewStyleMenu;

	/// <summary>The sort field the rail was last built for ("Title", "Artists", "Album", …).</summary>
	protected string? _lastSortBy;
	private bool _scrubberWired;

	/// <summary>The flyout holding the compact/list view style radio items.</summary>
	protected MenuFlyout ViewStyleMenu => _viewStyleMenu ??= RequiredControlFlyout("ViewStyle");

	private MenuFlyout RequiredControlFlyout(string name) =>
		FindName(name) as MenuFlyout ?? throw new InvalidOperationException($"Control '{name}' was not found on {GetType().Name}.");

	/// <summary>The text of the currently checked item in the "View" group of the view style menu.</summary>
	protected string CurrentViewStyleText() =>
		ViewStyleMenu.Items.OfType<RadioMenuFlyoutItem>().Where(item => item.GroupName == "View" && item.IsChecked).Select(item => item.Text).FirstOrDefault() ?? "Compact View";

	/// <summary>Scrolls to a specific song in the active view and selects it.</summary>
	/// <param name="song">The song object to scroll to. If null, no action is performed.</param>
	protected async Task ScrollToSong(Song? song)
	{
		var listView = GetCurrentViewStyle();
		if (song != null)
		{
			try
			{
				await listView.SmoothScrollIntoViewWithItemAsync(song, itemPlacement: ScrollItemPlacement.Center, disableAnimation: false, scrollIfVisible: false);
			}
			catch (Exception)
			{
			}
			listView.SelectedItem = song;
		}
	}

	/// <summary>
	/// Handles selection changes in the song views: updates multi-select button state or tracks the selected song.
	/// </summary>
	protected void SongListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var listView = GetCurrentViewStyle();
		if (listView.IsMultiSelectCheckBoxEnabled)
		{
			MoreButtonControl.IsEnabled = listView.SelectedItems.Count > 0;
		}
		else
			selectedSong = listView.SelectedItem as Song;
	}

	/// <summary>
	/// Handles the "Play All" action: disables shuffle, loads every displayed song into the player
	/// as the current playlist and scrolls to the first song.
	/// </summary>
	protected async void PlayAll_OnClick(object sender, RoutedEventArgs e)
	{
		ShuffleAndPlayControl.IsEnabled = false;
		var first = ViewModel.PlayAll(PlaylistKey);
		await ScrollToSong(first);
		ShuffleAndPlayControl.IsEnabled = true;
	}

	/// <summary>
	/// Handles the "Shuffle and Play" action: enables shuffle, loads every displayed song into the player
	/// starting from a randomly chosen song and scrolls to it.
	/// </summary>
	/// <remarks>
	/// SmoothScrollIntoViewWithItemAsync can race the layout pass that follows LoadPlaylist, so the first
	/// scroll is repeated after a short delay to guarantee the starting song is brought into view.
	/// </remarks>
	protected async void ShuffleAndPlay_OnClick(object sender, RoutedEventArgs e)
	{
		ShuffleAndPlayControl.IsEnabled = false;
		var starting = ViewModel.ShuffleAndPlay(PlaylistKey);
		await ScrollToSong(starting);       //somehow this doesn't work
		await Task.Delay(500);
		await ScrollToSong(starting);
		ShuffleAndPlayControl.IsEnabled = true;
	}

	/// <summary>
	/// Populates the alphabet scrubber with letters and optionally a special character marker
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
	protected void PopulateAlphabetNavigation(IOrderedEnumerable<string>? availableLetters, bool order, string sortBy, bool hasSpecialCharacters)
	{
		_lastSortBy = sortBy;

		var letters = new List<string>(Enumerable.Range('A', 26).Select(x => ((char)x).ToString()));
		if (hasSpecialCharacters) letters.Insert(0, "#");
		if (!order) letters.Reverse();

		var hasLetters = availableLetters != null || hasSpecialCharacters;
		var available = new List<string>();
		if (hasLetters && availableLetters != null)
			available.AddRange(availableLetters.Where(letters.Contains));
		if (hasSpecialCharacters)
			available.Add("#");

		Scrubber.SetLetters(letters, available, hasLetters);
		Scrubber.TargetList = GetCurrentViewStyle();
		WireScrubberEvents();

		InvalidateScrubberHighlightFeed();
		_ = AdjustAlphabetSize();
	}

	/// <summary>Wires the scrubber events once: tap → section jump, scroll → highlight updates.</summary>
	private void WireScrubberEvents()
	{
		if (_scrubberWired) return;
		_scrubberWired = true;

		Scrubber.LetterTapped += (_, letter) => ScrollToSection(letter, _lastSortBy ?? "Title");
		Scrubber.ScrollProgress += (_, progress) => OnScrubberScrollProgress(progress.Offset, progress.Viewport);
	}

	/// <summary>The list the scrubber tracks for scroll highlights — the currently active view style.</summary>
	protected override ListViewBase? ScrubberTarget => GetCurrentViewStyle();

	/// <summary>
	/// Maps a displayed item to its alphabet section letter ("A"–"Z" or "#") based on the active sort field.
	/// Rail pages override this; the default (by title) only backs derived pages without a rail.
	/// </summary>
	/// <param name="item">The item whose section letter is needed.</param>
	protected override string LetterForItem(object item)
		=> item is Song song ? FirstLetter(song.Title) : "#";

	/// <summary>
	/// Scrolls the view to the first song of the given alphabet section.
	/// Only pages with an A-Z panel override this.
	/// </summary>
	/// <param name="letter">The starting letter of the section, or "#" for non-alphabetic entries.</param>
	/// <param name="sortBy">The property the song list is currently sorted by.</param>
	protected virtual void ScrollToSection(string letter, string sortBy)
	{
	}

	/// <summary>
	/// Repositions the scrubber for the active view style and re-targets it at the active list. Called on
	/// view switches, sort changes and page resizes; the control sizes its own letters to the height.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation of resizing the elements in the alphabet navigation panel.</returns>
	protected Task AdjustAlphabetSize()
	{
		Scrubber.Margin = CurrentViewStyleText() switch
		{
			"List View" => new Thickness(0, 50, 0, 10),
			"Compact View" => new Thickness(0, 10, 0, 10),
			_ => new Thickness(0, 10, 0, 10)
		};
		Scrubber.TargetList = GetCurrentViewStyle();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Populates the "Add to playlist" submenu of the song context menu with all playlists known to the database.
	/// </summary>
	/// <remarks>
	/// If no playlists exist, a single disabled-looking "No Playlists created" item is shown instead.
	/// The method clears existing submenu entries before adding fresh ones on every open.
	/// </remarks>
	protected async void MenuFlyoutOpened(object sender, object e)
	{
		var menu = sender as MenuFlyout;
		var addToPlaylist = menu?.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault();

		addToPlaylist?.Items.Clear();

		List<string> playLists = await DatabaseHelper.Instance.GetAllPlaylistNames();

		var createNewMenu = new MenuFlyoutItem
		{
			Text = "Create/Add New playlist",
			Icon = new FontIcon { Glyph = "\uF8AA" },
		};
		ToolTipService.SetToolTip(createNewMenu, "Create/Add a new playlist and add the selected songs/tracks to it.");
		createNewMenu.Click += NewPlaylist_Click;
		addToPlaylist?.Items.Add(createNewMenu);

		foreach (var playList in playLists)
		{
			var menuItem = new MenuFlyoutItem
			{
				Text = playList,
				Icon = new FontIcon { Glyph = "\uE728" }
			};
			ToolTipService.SetToolTip(menuItem, BuildAddToPlaylistTooltip(MultiSelectToggle.IsChecked == true, playList));
			menuItem.Click += AddToPlaylist_Click;
			addToPlaylist?.Items.Add(menuItem);
		}
	}

	/// <summary>Adds the currently selected song(s) to the chosen playlist.</summary>
	protected abstract void AddToPlaylist_Click(object sender, RoutedEventArgs e);

	/// <summary>Builds the tooltip text shown on an "Add to playlist" menu entry.</summary>
	protected abstract string BuildAddToPlaylistTooltip(bool multiSelect, string playList);

	/// <summary>Opens the Add Playlist dialog on MainPage so the user can create/import a playlist.</summary>
	protected abstract void NewPlaylist_Click(object sender, RoutedEventArgs e);
}
