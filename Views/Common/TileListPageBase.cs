using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

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

	private bool _scrubberWired;

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
	/// Populates the alphabet scrubber with letters and optionally a special character marker
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
	protected void PopulateAlphabetNavigation(IOrderedEnumerable<string>? availableLetters, bool order, bool hasSpecialCharacters)
	{
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
		Scrubber.TargetList = TileView;
		WireScrubberEvents();
		_ = AdjustAlphabetSize();
	}

	/// <summary>Wires the scrubber events once: tap → section jump, scroll → highlight/bubble updates.</summary>
	private void WireScrubberEvents()
	{
		if (_scrubberWired) return;
		_scrubberWired = true;

		Scrubber.LetterTapped += (_, letter) => ScrollToSection(letter);
		Scrubber.ScrollProgress += (_, progress) => OnScrubberScrollProgress(progress.Offset, progress.Viewport);
	}

	/// <summary>Feeds the scrubber from list scrolling: highlights every letter with tiles on screen.</summary>
	private void OnScrubberScrollProgress(double offset, double viewport)
		=> Scrubber.SetHighlightedLetters(ComputeVisibleLetters(TileView, offset, viewport));

	/// <summary>
	/// Maps the realized containers of the virtualized grid onto their section letters — only on-screen
	/// tiles are realized, so this stays correct and cheap for any grid length or tile size.
	/// </summary>
	private List<string> ComputeVisibleLetters(ListViewBase view, double offset, double viewport)
	{
		var visible = new List<string>();
		var panel = view.ItemsPanelRoot;
		if (panel == null || viewport <= 0) return visible;

		foreach (var container in panel.Children)
		{
			if (container is not FrameworkElement element) continue;
			var slot = LayoutInformation.GetLayoutSlot(element);
			if (slot.Bottom <= offset || slot.Top >= offset + viewport) continue;

			var item = view.ItemFromContainer(element);
			if (item != null)
				visible.Add(LetterForItem(item));
		}
		return visible.Distinct().ToList();
	}

	/// <summary>
	/// Maps a displayed tile to its alphabet section letter ("A"–"Z" or "#"). Tile pages override this;
	/// the default only backs derived pages without a rail (e.g. YearsViewPage).
	/// </summary>
	/// <param name="item">The tile whose section letter is needed.</param>
	protected virtual string LetterForItem(object item) => "#";

	/// <summary>
	/// Scrolls the view to the first tile of the given alphabet section.
	/// Only pages with an A-Z panel override this.
	/// </summary>
	/// <param name="letter">The starting letter of the section, or "#" for non-alphabetic entries.</param>
	protected virtual void ScrollToSection(string letter)
	{
	}

	/// <summary>
	/// Repositions the scrubber and re-targets it at the tile grid. Called on sort changes and page
	/// resizes; the control sizes its own letters to the height.
	/// </summary>
	/// <returns>A task that represents the asynchronous operation of resizing the elements in the alphabet navigation panel.</returns>
	protected Task AdjustAlphabetSize()
	{
		Scrubber.Margin = new Thickness(0, 10, 0, 10);
		Scrubber.TargetList = TileView;
		return Task.CompletedTask;
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
