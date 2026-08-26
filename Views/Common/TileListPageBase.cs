using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Views.Common;

/// <summary>
/// Shared base for library tile-grid pages (albums/artists/genres/years) built on <see cref="TunetasticPageBase"/>:
/// single-tile selection tracking, A-Z navigation and the shared "Add to playlist" context menu.
/// </summary>
public abstract partial class TileListPageBase : TunetasticPageBase
{
	/// <summary>The grid showing the page's tiles.</summary>
	protected abstract ListViewBase TileView { get; }

	/// <summary>The button whose IsEnabled reflects multi-select state.</summary>
	protected abstract Button MoreButtonControl { get; }

	/// <summary>
	/// Handles selection changes in the tile view: updates the multi-select button state.
	/// </summary>
	protected void TileView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (TileView.IsMultiSelectCheckBoxEnabled)
		{
			MoreButtonControl.IsEnabled = TileView.SelectedItems.Count > 0;
		}
	}

	/// <summary>
	/// Populates the alphabet navigation panel with letters and optionally a special character marker
	/// for navigating sections of tiles.
	/// </summary>
	/// <param name="availableLetters">
	/// A collection of letters representing sections to be included in navigation. Null indicates
	/// all letters are marked as unavailable.
	/// </param>
	/// <param name="order">
	/// A flag indicating whether the letters are ordered in ascending or descending order.
	/// </param>
	/// <param name="hasSpecialCharacters">
	/// A flag specifying whether special characters (e.g., "#") are included in the navigation.
	/// </param>
	protected async void PopulateAlphabetNavigation(IOrderedEnumerable<string>? availableLetters, bool order, bool hasSpecialCharacters)
	{
		AlphabetPanel.Children.Clear();
		if (availableLetters == null && !hasSpecialCharacters) return;

		var fullAlphabet = Enumerable.Range('A', 26).Select(x => ((char)x).ToString());
		if (hasSpecialCharacters) fullAlphabet = fullAlphabet.Reverse().Append("#").Reverse();
		if (!order) fullAlphabet = fullAlphabet.Reverse();

		double availableSpace = ContentAreaGrid.ActualHeight - 10;
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
				Button.Tapped += (s, e) => ScrollToSection(letter);
			}

			AlphabetPanel.Children.Add(Button);
		}
		_ = AdjustAlphabetSize();
		availableLetters = null;
	}

	/// <summary>
	/// Scrolls the view to the first tile of the given alphabet section.
	/// Only pages with an A-Z panel override this.
	/// </summary>
	/// <param name="letter">The starting letter of the section, or "#" for non-alphabetic entries.</param>
	protected virtual void ScrollToSection(string letter)
	{
	}

	/// <summary>
	/// Adjusts the size of the elements in the alphabet navigation panel based on the available vertical space.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation of resizing the elements in the alphabet navigation panel.</returns>
	protected Task AdjustAlphabetSize()
	{
		double availableSpace = ContentAreaGrid.ActualHeight - 20;

		AlphabetPanel.Margin = new Thickness(0, 10, 30, 10);

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
	/// Populates the "Add to playlist" submenu of the tile context menu with all playlists known to the database.
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

	/// <summary>Adds the songs of the currently selected tile(s) to the chosen playlist.</summary>
	protected abstract void AddToPlaylist_Click(object sender, RoutedEventArgs e);

	/// <summary>Builds the tooltip text shown on an "Add to playlist" menu entry.</summary>
	protected abstract string BuildAddToPlaylistTooltip(bool multiSelect, string playList);
}
