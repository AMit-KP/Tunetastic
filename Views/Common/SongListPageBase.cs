using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media;

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
	protected async void PopulateAlphabetNavigation(IOrderedEnumerable<string>? availableLetters, bool order, string sortBy, bool hasSpecialCharacters)
	{
		AlphabetPanel.Children.Clear();
		if (availableLetters == null && !hasSpecialCharacters) return;

		var fullAlphabet = Enumerable.Range('A', 26).Select(x => ((char)x).ToString());
		if (hasSpecialCharacters) fullAlphabet = fullAlphabet.Reverse().Append("#").Reverse();
		if (!order) fullAlphabet = fullAlphabet.Reverse();

		var viewStyle = CurrentViewStyleText();
		double availableSpace = ContentAreaGrid.ActualHeight - viewStyle switch
		{
			"List View" => 60,
			"Compact View" => 20,
			_ => 20
		};
		if (availableSpace <= 0) return;

		double autoHeight = availableSpace / fullAlphabet.Count();

		var transparentBrush = new SolidColorBrush(Colors.Transparent);

		foreach (var letter in fullAlphabet)
		{
			bool hasSongs = availableLetters == null ? false : (availableLetters.Contains(letter)) || (letter == "#" && hasSpecialCharacters);

			var foreground = new SolidColorBrush(hasSongs ? App.Current.ThemeService.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black : Colors.Gray);
			var Button = new Button
			{
				Content = letter,
				Foreground = foreground,
				Opacity = hasSongs ? 1 : 0.5,
				Background = transparentBrush,
				BorderBrush = transparentBrush,
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
			Button.Resources["ButtonBackgroundPointerOver"] = transparentBrush;
			Button.Resources["ButtonBackgroundPressed"] = transparentBrush;
			Button.Resources["ButtonForegroundPointerOver"] = foreground;
			Button.Resources["ButtonForegroundPressed"] = foreground;
			Button.Resources["ButtonBorderBrushPointerOver"] = transparentBrush;
			Button.Resources["ButtonBorderBrushPressed"] = transparentBrush;

			if (hasSongs)
			{
				ToolTipService.SetPlacement(Button, Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Left);
				ToolTipService.SetToolTip(Button, letter);
				Button.Tapped += (s, e) => ScrollToSection(letter, sortBy);
			}

			AlphabetPanel.Children.Add(Button);
		}
		_ = AdjustAlphabetSize();
		availableLetters = null;
	}

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
	/// Adjusts the size of the elements in the alphabet navigation panel based on the available vertical space.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation of resizing the elements in the alphabet navigation panel.</returns>
	protected Task AdjustAlphabetSize()
	{
		var viewStyle = CurrentViewStyleText();
		double availableSpace = ContentAreaGrid.ActualHeight - viewStyle switch
		{
			"List View" => 60,
			"Compact View" => 20,
			_ => 20
		};

		AlphabetPanel.Margin = viewStyle switch
		{
			"List View" => new Thickness(0, 50, 30, 10),
			"Compact View" => new Thickness(0, 10, 30, 10),
			_ => new Thickness(0, 10, 30, 10)
		};

		if (availableSpace <= 0) return Task.CompletedTask;

		double totalLetters = AlphabetPanel.Children.Count;

		double autoHeight = availableSpace / totalLetters;

		foreach (var button in AlphabetPanel.Children.OfType<Button>())
		{
			button.Height = autoHeight;
		}
		return Task.CompletedTask;
	}

	/// <summary>
	/// Re-colors enabled alphabet navigation buttons when the page theme changes.
	/// </summary>
	protected void ApplyAlphabetThemeColors(FrameworkElement sender, object args)
	{
		Brush themeBrush = (sender.ActualTheme == ElementTheme.Dark) ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
		AlphabetPanel.Children.OfType<Button>().Where(button => button.Opacity == 1).ToList().ForEach(textElement => textElement.Foreground = themeBrush);
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
			ToolTipService.SetToolTip(menuItem, BuildAddToPlaylistTooltip(MultiSelectToggle.IsChecked == true, playList));
			menuItem.Click += AddToPlaylist_Click;
			addToPlaylist?.Items.Add(menuItem);
		}
	}

	/// <summary>Adds the currently selected song(s) to the chosen playlist.</summary>
	protected abstract void AddToPlaylist_Click(object sender, RoutedEventArgs e);

	/// <summary>Builds the tooltip text shown on an "Add to playlist" menu entry.</summary>
	protected abstract string BuildAddToPlaylistTooltip(bool multiSelect, string playList);
}
