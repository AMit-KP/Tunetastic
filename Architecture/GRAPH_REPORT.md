# Graph Report - Tunetastic  (2026-08-31)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 2770 nodes · 5306 edges · 163 communities (134 shown, 29 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 257 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `26b002c3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- NativeMethods
- TextBlock
- TileListPageBase
- Tunetastic
- .Info
- MusicControlViewModel.cs
- .MakeNextButton
- AlbumsViewPage
- Page
- TileListPageBase
- TaskbarOverlayWindow
- Page
- TaskbarOverlayManager
- TaskbarInfo
- RecentlyAdded
- MainPlayerPage
- DatabaseHelper
- Tunetastic.Overlay.Layouts
- MainPage
- SettingsPage
- Enums.cs
- AllSongsViewPage
- SongListPageBase
- SmoothProgressBar
- SongListPageBase
- InlineSuggestBox
- RecentlyPlayed
- .ScrollToSong
- GenreDetailPage
- YearDetailPage
- SongListPageBase
- SongListPageBase
- RoutedEventArgs
- App
- RadioMenuFlyoutItem
- SongListPageBase
- SongListPageBase
- SongListPageBase
- ArtistDetailPage
- SongListPageBase
- AlbumTintOverlay
- Rectangle
- RoutedEventArgs
- Page
- MenuFlyout
- PlayListTemplate
- DropDownButton
- ArtistsViewPage
- MostPlayed
- DiskSpeedDetector
- List
- AlbumDetailPage
- MusicPlayer
- ContentDialog
- TileListPageBase
- AudioService
- AlbumTintProgressOverlay
- MainWindow
- Button
- Type
- FullscreenStateService
- RoutedEventArgs
- TileListPageBase
- RoutedEventArgs
- RoutedEventArgs
- Tunetastic.Views.Common
- TunetasticPageBase
- SessionEventHandler
- AccentAncientScrollOverlay
- Tunetastic.Common.Helpers
- ColorHelper
- WindowsMediaBackend
- .Search
- TileListPageBase
- MenuFlyoutSubItem
- .GetSuggestions
- GlobalUsings.cs
- ImportExportPlaylist
- FlyleafMediaBackend
- .TileView_SelectionChanged
- .EditInfoSaveButtonEnableUpdate
- RoutedEventArgs
- SmartWrapVirtualizingLayout
- LrcParser
- .OnDeviceStateChanged
- .SyncSongArtistsForSong
- ShellNotificationWindow
- RoutedEventArgs
- PlaybackTracker
- TopAlbumAccentStripeOverlay
- .ReloadArtistSplitRules
- SettingViewModel
- RoutedEventArgs
- IMediaBackend
- .LoadSong
- ListView
- ComboBox
- Song
- MusicControl
- OverlayBase
- .UpdateListBasedOnSorting
- .UpdateListBasedOnSorting
- .UpdateListBasedOnSorting
- .UpdateListBasedOnSorting
- .UpdateListBasedOnSorting
- .ProcessPendingTagWritesAsync
- Tunetastic.Views
- StackPanel
- .ApplyAndSaveTint
- .FindAllAppSessions
- Slider
- .UpdateListBasedOnViewStyle
- .PlayAllButton_OnClick
- BuyMeACoffee
- .WaitAndSubscribeToAppVolumeAsync
- .ScanLibraries
- PlaybackStateChangedArgs
- SongListViewModel
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .ListView_SelectionChanged
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- TextOnlyReversedOverlay
- .Artist_Tapped
- ArtistTileView
- .Genre_Tapped
- .Year_Tapped
- AppIcon
- SettingsExpander
- PART_TextBox
- AutoScrollView
- QueuedList
- AppData.json
- .NumberBox_ValueChanged
- .AnimateLyricButton
- .ApplyOffsetLive
- .ViewButton_OnClick
- .TimeStyle_Click
- Converters.xaml
- Fonts.xaml
- ThemeResources.xaml
- .OnNavigatingFrom
- .CreateFreshPage
- .OnNavigatedTo
- .Page_SizeChanged
- .Page_SizeChanged
- IgnoretracksDuration
- .ViewButton_OnClick
- .TaskBarOverlay_Toggled
- ExportPlayList
- PauseOnMute

## God Nodes (most connected - your core abstractions)
1. `DatabaseHelper` - 88 edges
2. `TextBlock` - 83 edges
3. `Page` - 77 edges
4. `RadioMenuFlyoutItem` - 73 edges
5. `StackPanel` - 70 edges
6. `Page` - 67 edges
7. `SettingsPage` - 65 edges
8. `MainPage` - 63 edges
9. `NativeMethods` - 57 edges
10. `MainPlayerPage` - 56 edges

## Surprising Connections (you probably didn't know these)
- `TaskbarOverlayWindow` --inherits--> `WindowEx`  [EXTRACTED]
  Common/Services/TaskbarOverlay/TaskbarOverlayWindow.cs → MainWindow.xaml
- `App` --inherits--> `Application`  [EXTRACTED]
  App.xaml.cs → App.xaml
- `MainWindow` --inherits--> `WindowEx`  [EXTRACTED]
  MainWindow.xaml.cs → MainWindow.xaml
- `ActualAlbumGroup` --references--> `TextBlock`  [EXTRACTED]
  Views/LibraryViews/AlbumDetailPage.xaml → Views/MainPage.xaml
- `DeleteDialogText` --references--> `TextBlock`  [EXTRACTED]
  Views/LibraryViews/AlbumDetailPage.xaml → Views/MainPage.xaml

## Import Cycles
- None detected.

## Communities (163 total, 29 thin omitted)

### Community 0 - "NativeMethods"
Cohesion: 0.08
Nodes (25): APPBARDATA, int, IntPtr, string, ABE, ABM, ABS, APPBARDATA (+17 more)

### Community 1 - "TextBlock"
Cohesion: 0.05
Nodes (62): Key, Value, DeleteDialogText, GoToSettingsTextBlock, ProgressFillText, AddPlaylistDialogDescription, AlbumChanged, AlbumGrid (+54 more)

### Community 2 - "TileListPageBase"
Cohesion: 0.06
Nodes (32): AlphabetNavigationPanel, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText, Descending, GenreTextBlock, GoToSettings (+24 more)

### Community 3 - "Tunetastic"
Cohesion: 0.04
Nodes (55): net9.0-windows10.0.26100.0, CommunityToolkit.Common (8.4.2), CommunityToolkit.HighPerformance (8.4.2), CommunityToolkit.Labs.WinUI.Controls.DataTable (0.1.251217-build.2433), CommunityToolkit.Labs.WinUI.Shimmer (0.1.250811-build.2202), CommunityToolkit.Mvvm (8.4.2), CommunityToolkit.WinUI.Animations (8.2.251219), CommunityToolkit.WinUI.Behaviors (8.2.251219) (+47 more)

### Community 4 - ".Info"
Cohesion: 0.09
Nodes (15): MultiSelectButton, bool, Button, ContainerContentChangingEventArgs, List, ListViewBase, NavigatingCancelEventArgs, NavigationEventArgs (+7 more)

### Community 5 - "MusicControlViewModel.cs"
Cohesion: 0.06
Nodes (41): Tunetastic.Overlay, OverlayLayout, OverlayTheme, OverlayFactory, IReadOnlyList, OverlayLayoutCatalog, OverlayLayoutInfo, PlaybackStateChangedArgs (+33 more)

### Community 6 - ".MakeNextButton"
Cohesion: 0.13
Nodes (32): Grid, Grid, Build(), Grid, Build(), Grid, Build(), Grid (+24 more)

### Community 7 - "AlbumsViewPage"
Cohesion: 0.07
Nodes (21): Ascending, Descending, MultiSelectButton, AlbumModel, bool, Button, ContainerContentChangingEventArgs, double (+13 more)

### Community 8 - "Page"
Cohesion: 0.07
Nodes (38): FontSize, AboutTextBlock, AlbumsToggle, AppearanceTextBlock, ArtistsToggle, AudioTextBlock, AutoAdvanceCard, AutoAdvanceSwitch (+30 more)

### Community 9 - "TileListPageBase"
Cohesion: 0.06
Nodes (35): AlbumCover, AlbumTextBlock, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettingsTextBlock, More, MoreButton (+27 more)

### Community 10 - "TaskbarOverlayWindow"
Cohesion: 0.08
Nodes (17): OverlayRect, bool, DispatcherTimer, Grid, int, IntPtr, PointerRoutedEventArgs, SUBCLASSPROC (+9 more)

### Community 11 - "Page"
Cohesion: 0.06
Nodes (41): Album, Artist, BackgroundImage, BlurBorder, BlurEffect, CoverArt, CoverArtImage, CoverArtProjection (+33 more)

### Community 12 - "TaskbarOverlayManager"
Cohesion: 0.08
Nodes (16): bool, Brush, Dictionary, DispatcherQueue, DispatcherTimer, int, IntPtr, IReadOnlyList (+8 more)

### Community 13 - "TaskbarInfo"
Cohesion: 0.11
Nodes (18): ABE, Dictionary, HashSet, int, IntPtr, IReadOnlyList, List, FreeZone (+10 more)

### Community 14 - "RecentlyAdded"
Cohesion: 0.09
Nodes (13): DateStyle, RecentlyAddedPage, RelativeTimeStyle, Button, DispatcherTimer, List, ListView, ObservableCollection (+5 more)

### Community 15 - "MainPlayerPage"
Cohesion: 0.09
Nodes (15): Compositor, MusicPlayer, CloseLyricsButton, BitmapImage, bool, DispatcherQueue, DispatcherTimer, double (+7 more)

### Community 16 - "DatabaseHelper"
Cohesion: 0.10
Nodes (9): HashSet, LibraryModel, Regex, Task, AlbumNameRow, ArtistNameRow, DatabaseHelper, SearchCategory (+1 more)

### Community 17 - "Tunetastic.Overlay.Layouts"
Cohesion: 0.06
Nodes (20): Tunetastic.Overlay.Layouts, BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage (+12 more)

### Community 18 - "MainPage"
Cohesion: 0.08
Nodes (12): NavigationView, NavigationViewItemInvokedEventArgs, NavigationViewSelectionChangedEventArgs, RectInt32, ArtistSplitRule, bool, FrameworkElement, List (+4 more)

### Community 19 - "SettingsPage"
Cohesion: 0.09
Nodes (11): SettingViewModel, DependencyPropertyChangedEventArgs, FrameworkElement, LibraryModel, MusicFormatModel, ObservableCollection, OverlayLayout, RangeBaseValueChangedEventArgs (+3 more)

### Community 20 - "Enums.cs"
Cohesion: 0.07
Nodes (33): string, Constants, ArtistRuleType, BackendType, DiskKind, FadeType, LocalSave, OverlayLayout (+25 more)

### Community 21 - "AllSongsViewPage"
Cohesion: 0.09
Nodes (13): MultiSelectButton, Button, FrameworkElement, List, ListView, NavigationEventArgs, ObservableCollection, Page (+5 more)

### Community 22 - "SongListPageBase"
Cohesion: 0.08
Nodes (29): CompactViewStyle, CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100 (+21 more)

### Community 23 - "SmoothProgressBar"
Cohesion: 0.10
Nodes (15): bool, Brush, DependencyProperty, DependencyPropertyChangedEventArgs, DispatcherTimer, double, Grid, PointerRoutedEventArgs (+7 more)

### Community 24 - "SongListPageBase"
Cohesion: 0.09
Nodes (29): RelativeTime, BoolToVisibility, IsRelativeDisplayMode, CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettings, GoToSettingsTextBlock (+21 more)

### Community 25 - "InlineSuggestBox"
Cohesion: 0.11
Nodes (17): ArtistSplitRule, bool, DependencyProperty, int, KeyRoutedEventArgs, List, PointerRoutedEventArgs, RoutedEventArgs (+9 more)

### Community 26 - "RecentlyPlayed"
Cohesion: 0.11
Nodes (11): SongListPageBase, RecentlyPlayedPage, Button, DispatcherTimer, List, NavigationEventArgs, ObservableCollection, Page (+3 more)

### Community 27 - ".ScrollToSong"
Cohesion: 0.11
Nodes (11): MenuFlyout, SongListViewModel, Button, FrameworkElement, IOrderedEnumerable, ListView, RoutedEventArgs, SelectionChangedEventArgs (+3 more)

### Community 28 - "GenreDetailPage"
Cohesion: 0.11
Nodes (10): MultiSelectButton, Button, FrameworkElement, ListView, NavigatingCancelEventArgs, ObservableCollection, Page, RoutedEventArgs (+2 more)

### Community 29 - "YearDetailPage"
Cohesion: 0.11
Nodes (10): MultiSelectButton, Button, FrameworkElement, ListView, NavigatingCancelEventArgs, ObservableCollection, Page, RoutedEventArgs (+2 more)

### Community 30 - "SongListPageBase"
Cohesion: 0.09
Nodes (25): ActualAlbumGroup, AlbumDetailCompactViewGrid, AlbumDetailListViewGrid, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar (+17 more)

### Community 31 - "SongListPageBase"
Cohesion: 0.09
Nodes (26): ActualYearGroup, AlbumSort, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+18 more)

### Community 32 - "RoutedEventArgs"
Cohesion: 0.09
Nodes (14): AlbumTextBox, ArtistTextBox, BrowseCoverArtButton, ClearButton, GenreAutoSuggestBox, OpenContainingFolderButton, RemoveCoverArtButton, SearchBox (+6 more)

### Community 33 - "App"
Cohesion: 0.08
Nodes (17): Application, IntPtr, SystemTrayIcon, App, AudioService, Tunetastic, IJsonNavigationService, IRainbowFrame (+9 more)

### Community 34 - "RadioMenuFlyoutItem"
Cohesion: 0.13
Nodes (22): AlbumSort, AllSongsCompactViewGrid, AllSongsListViewGrid, ArtistsSort, Ascending, CompactViewStyle, ContentGrid, CustomProgressBar (+14 more)

### Community 35 - "SongListPageBase"
Cohesion: 0.09
Nodes (25): CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200, Limit50 (+17 more)

### Community 36 - "SongListPageBase"
Cohesion: 0.09
Nodes (24): CustomProgressBar, DeleteDialogText, ErrorMessage, ExportErrorMessage, ExportFormat, ExportTextBox, GoToSettings, GoToSettingsTextBlock (+16 more)

### Community 37 - "SongListPageBase"
Cohesion: 0.10
Nodes (23): ActualArtistGroup, AlbumSort, AlphabetNavigationPanel, ArtistDetailCompactViewGrid, ArtistDetailListViewGrid, Ascending, ContentGrid, CustomProgressBar (+15 more)

### Community 38 - "ArtistDetailPage"
Cohesion: 0.13
Nodes (8): MultiSelectButton, Button, FrameworkElement, ListView, ObservableCollection, RoutedEventArgs, Song, ArtistDetailPage

### Community 39 - "SongListPageBase"
Cohesion: 0.11
Nodes (23): ActualGenreGroup, AlbumSort, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+15 more)

### Community 40 - "AlbumTintOverlay"
Cohesion: 0.09
Nodes (19): AccentColorAnalyzer, BaseColorAnalyzer, Border, Button, ColorAnalyzer, ColorWeightAnalyzer, Image, Rectangle (+11 more)

### Community 41 - "Rectangle"
Cohesion: 0.08
Nodes (23): ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill (+15 more)

### Community 42 - "RoutedEventArgs"
Cohesion: 0.08
Nodes (8): CheckForUpdates, ForwardRewindButtonVisibility, IgnoreDup, MinimizeToTray, UseSystemVolume, VersionInfoToggle, RoutedEventArgs, ProgressButton

### Community 43 - "Page"
Cohesion: 0.10
Nodes (22): DurationConverter, Position, AlbumCover, BottomProgressBar, CurrentDurationConverter, ForwardButton, MusicControls, NextButton (+14 more)

### Community 44 - "MenuFlyout"
Cohesion: 0.09
Nodes (23): More, Sort, ViewStyle, More, Sort, ViewStyle, More, Sort (+15 more)

### Community 45 - "PlayListTemplate"
Cohesion: 0.12
Nodes (8): Button, List, NavigationEventArgs, ObservableCollection, Page, Song, TextChangedEventArgs, PlayListTemplate

### Community 46 - "DropDownButton"
Cohesion: 0.10
Nodes (21): SortDropDown, ViewButton, SortDropDown, ViewButton, SortDropDown, ViewButton, SortDropDown, ViewButton (+13 more)

### Community 47 - "ArtistsViewPage"
Cohesion: 0.11
Nodes (11): bool, Button, ContainerContentChangingEventArgs, FrameworkElement, List, ListViewBase, NavigatingCancelEventArgs, ObservableCollection (+3 more)

### Community 48 - "MostPlayed"
Cohesion: 0.13
Nodes (8): Button, List, NavigationEventArgs, ObservableCollection, Page, Song, Task, MostPlayed

### Community 49 - "DiskSpeedDetector"
Cohesion: 0.32
Nodes (4): TimeSpan, DiskSpeedDetector, DiskKind, Func

### Community 50 - "List"
Cohesion: 0.10
Nodes (7): AlbumModel, ArtistModel, ArtistSplitRule, GenreModel, List, MusicFormatModel, YearModel

### Community 51 - "AlbumDetailPage"
Cohesion: 0.11
Nodes (10): Button, FrameworkElement, NavigatingCancelEventArgs, NavigationEventArgs, ObservableCollection, Page, SizeChangedEventArgs, Song (+2 more)

### Community 52 - "MusicPlayer"
Cohesion: 0.13
Nodes (13): BackendType, bool, IMediaBackend, int, MediaPlayer, Player, RepeatMode, ShuffleMode (+5 more)

### Community 53 - "ContentDialog"
Cohesion: 0.10
Nodes (20): DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog (+12 more)

### Community 54 - "TileListPageBase"
Cohesion: 0.11
Nodes (19): AlphabetNavigationPanel, ArtistTextBlock, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, LoadingProgress (+11 more)

### Community 55 - "AudioService"
Cohesion: 0.11
Nodes (7): AudioVolumeNotificationData, bool, AudioService, IMMNotificationClient, MMDeviceEnumerator, object, PropertyKey

### Community 56 - "AlbumTintProgressOverlay"
Cohesion: 0.12
Nodes (16): AccentColorAnalyzer, BaseColorAnalyzer, Border, Brush, Button, Color, ColorAnalyzer, ColorWeightAnalyzer (+8 more)

### Community 57 - "MainWindow"
Cohesion: 0.14
Nodes (8): AppWindow, WindowEx, SystemTrayIcon, SystemTrayIconEventArgs, void, MainWindow, OverlappedPresenter, WindowEventArgs

### Community 58 - "Button"
Cohesion: 0.25
Nodes (6): AcceptSyncButton, CancelSyncButton, LyricsMenuButton, MusicInfoButton, ShowLyricsButton, Button

### Community 59 - "Type"
Cohesion: 0.19
Nodes (7): Type, DateFormatConverter, DurationConverter, DurationToFullTimeConverter, RelativeTimeConverter, Tunetastic.Common.Converters, IValueConverter

### Community 60 - "FullscreenStateService"
Cohesion: 0.19
Nodes (5): Dictionary, DispatcherTimer, IReadOnlyList, FullscreenStateService, TaskbarStateService

### Community 61 - "RoutedEventArgs"
Cohesion: 0.14
Nodes (6): MultiSelectButton, ListView, RoutedEventArgs, ShuffleButton, ToggleButton, MultiSelectButton

### Community 62 - "TileListPageBase"
Cohesion: 0.09
Nodes (23): CommonStates, CommonStates, CommonStates, Ascending, CommonStates, ContentGrid, CustomProgressBar, DeleteDialogText (+15 more)

### Community 63 - "RoutedEventArgs"
Cohesion: 0.12
Nodes (10): ClearLyricsButton, CopyLyricsButton, EditLyricsButton, OpenLyricsButton, SaveAsOffsetButton, SaveByTimestampsButton, SyncLyricsButton, RoutedEventArgs (+2 more)

### Community 64 - "RoutedEventArgs"
Cohesion: 0.15
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 65 - "Tunetastic.Views.Common"
Cohesion: 0.22
Nodes (3): Tunetastic.Views.PlaylistViews, Tunetastic.Views.LibraryViews, Tunetastic.Views.Common

### Community 66 - "TunetasticPageBase"
Cohesion: 0.17
Nodes (10): Page, StackPanel, ToggleButton, DispatcherQueue, Grid, List, Rectangle, Task (+2 more)

### Community 67 - "SessionEventHandler"
Cohesion: 0.14
Nodes (7): AudioSessionDisconnectReason, AudioSessionState, IntPtr, SessionEventHandler, Guid, IAudioSessionEventsHandler, SessionEventHandler

### Community 68 - "AccentAncientScrollOverlay"
Cohesion: 0.17
Nodes (11): await, LinearGradientBrush, AccentColorAnalyzer, Border, Color, ColorAnalyzer, Image, SolidColorBrush (+3 more)

### Community 69 - "Tunetastic.Common.Helpers"
Cohesion: 0.14
Nodes (9): AppConfig, AppHelper, ImageResizer, Tunetastic.Common.Helpers, IPicture, IVersionable, NotifiyingJsonSettings, ThumbnailFolder (+1 more)

### Community 70 - "ColorHelper"
Cohesion: 0.26
Nodes (6): Color, double, OverlayTheme, ColorHelper, Lab, Lab

### Community 71 - "WindowsMediaBackend"
Cohesion: 0.15
Nodes (7): bool, double, MediaPlayer, Task, WindowsMediaBackend, MediaPlaybackSession, Timer

### Community 72 - ".Search"
Cohesion: 0.20
Nodes (5): IEnumerable, SearchCategory, SearchItem, SearchResults, SearchScope

### Community 73 - "TileListPageBase"
Cohesion: 0.16
Nodes (7): Button, FrameworkElement, IOrderedEnumerable, ListViewBase, RoutedEventArgs, Task, TileListPageBase

### Community 74 - "MenuFlyoutSubItem"
Cohesion: 0.13
Nodes (15): AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist (+7 more)

### Community 75 - ".GetSuggestions"
Cohesion: 0.19
Nodes (7): AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs, AutoSuggestBoxSuggestionChosenEventArgs, AutoSuggestBoxTextChangedEventArgs, KeyValuePair, SearchItemType, Task

### Community 76 - "GlobalUsings.cs"
Cohesion: 0.15
Nodes (4): GlobalNotification, Tunetastic.ViewModels, Tunetastic.Common.Infrastructure, Tunetastic.Common.Controls

### Community 77 - "ImportExportPlaylist"
Cohesion: 0.22
Nodes (6): List, name, Task, ImportExportPlaylist, totalTrackCount, trackInLibrary

### Community 78 - "FlyleafMediaBackend"
Cohesion: 0.14
Nodes (7): bool, long, Player, Task, FlyleafMediaBackend, OpenCompletedArgs, PropertyChangedEventArgs

### Community 79 - ".TileView_SelectionChanged"
Cohesion: 0.14
Nodes (8): SelectionChangedEventArgs, SelectionChangedEventArgs, GenreTileView, ItemClickEventArgs, SelectionChangedEventArgs, ItemClickEventArgs, SelectionChangedEventArgs, YearTileView

### Community 80 - ".EditInfoSaveButtonEnableUpdate"
Cohesion: 0.24
Nodes (6): LyricsTextBox, PlaylistNameBox, TitleTextBox, TextChangedEventArgs, TextBox, YearNumberBox

### Community 81 - "RoutedEventArgs"
Cohesion: 0.19
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 82 - "SmartWrapVirtualizingLayout"
Cohesion: 0.24
Nodes (8): double, FrameworkElement, int, Size, LayoutState, SmartWrapVirtualizingLayout, VirtualizingLayout, VirtualizingLayoutContext

### Community 83 - "LrcParser"
Cohesion: 0.26
Nodes (6): TimeSpan, LrcLine, List, Regex, LrcParser, Match

### Community 84 - ".OnDeviceStateChanged"
Cohesion: 0.23
Nodes (4): DataFlow, DeviceState, MMDevice, Role

### Community 86 - "ShellNotificationWindow"
Cohesion: 0.18
Nodes (6): bool, Dictionary, DispatcherQueue, ShellNotificationWindow, Tunetastic.Common.Services.TaskbarOverlay, WndProcDelegate

### Community 87 - "RoutedEventArgs"
Cohesion: 0.19
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 88 - "PlaybackTracker"
Cohesion: 0.18
Nodes (4): DateTime, TimeSpan, PlaybackTracker, Tunetastic.Common.Services

### Community 89 - "TopAlbumAccentStripeOverlay"
Cohesion: 0.17
Nodes (11): AccentColorAnalyzer, BaseColorAnalyzer, Border, ColorAnalyzer, ColorWeightAnalyzer, double, Image, Rectangle (+3 more)

### Community 91 - "SettingViewModel"
Cohesion: 0.33
Nodes (5): bool, RelayCommand, string, Task, SettingViewModel

### Community 93 - "IMediaBackend"
Cohesion: 0.20
Nodes (4): Task, IMediaBackend, Tunetastic.Common.Services.Backends, IDisposable

### Community 96 - "ListView"
Cohesion: 0.24
Nodes (7): AlbumDetailCompactView, AlbumDetailListView, ItemClickEventArgs, SelectionChangedEventArgs, AlbumTileView, ItemClickEventArgs, ListView

### Community 97 - "ComboBox"
Cohesion: 0.22
Nodes (9): ActualHeight, All, Backdrop, LRCOffsetStandard, TaskBarOverlayDesign, TaskBarOverlayPosition, TaskBarOverlayTheme, Theme (+1 more)

### Community 99 - "MusicControl"
Cohesion: 0.31
Nodes (3): MusicControlViewModel, Storyboard, MusicControl

### Community 100 - "OverlayBase"
Cohesion: 0.25
Nodes (5): bool, Grid, OverlayTheme, UIElement, OverlayBase

### Community 101 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 102 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 103 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (4): Ascending, Descending, ArtistModel, Task

### Community 104 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 105 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 106 - ".ProcessPendingTagWritesAsync"
Cohesion: 0.36
Nodes (3): List, Song, Task

### Community 107 - "Tunetastic.Views"
Cohesion: 0.25
Nodes (3): Tunetastic.Views, Task, SplashScreen

### Community 108 - "StackPanel"
Cohesion: 0.12
Nodes (17): AlphabetNavigationPanel, GoToSettings, LoadingProgress, PageButtons, SortAndViewButtonPanel, AlphabetNavigationPanel, GoToSettings, LoadingProgress (+9 more)

### Community 109 - ".ApplyAndSaveTint"
Cohesion: 0.29
Nodes (4): ColorChangedEventArgs, ColorPaletteColorChangedEventArgs, ColorPicker, Color

### Community 110 - ".FindAllAppSessions"
Cohesion: 0.33
Nodes (4): List, Name, Id, Pid

### Community 111 - "Slider"
Cohesion: 0.29
Nodes (7): VolumeSlider, Slider, AutoAdvanceSlider, MainPlayerBlurSlider, ManualTrackChangeSlider, PlayPauseStopFadeSlider, RainbowSpeedSlider

### Community 112 - ".UpdateListBasedOnViewStyle"
Cohesion: 0.33
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 113 - ".PlayAllButton_OnClick"
Cohesion: 0.29
Nodes (5): MoreButton, PlayAll, SettingsButton, ShuffleAndPlay, Button

### Community 114 - "BuyMeACoffee"
Cohesion: 0.29
Nodes (4): BuyMeACoffee, GitHubIssues, RateThisAppButton, Button

### Community 115 - ".WaitAndSubscribeToAppVolumeAsync"
Cohesion: 0.33
Nodes (3): AudioSessionControl, Task, IAudioSessionControl

### Community 116 - ".ScanLibraries"
Cohesion: 0.29
Nodes (5): bool, List, Task, GetMusicData, Tunetastic.Common.Operations

### Community 117 - "PlaybackStateChangedArgs"
Cohesion: 0.33
Nodes (3): PlaybackStateChangedArgs, EventArgs, PlaybackState

### Community 118 - "SongListViewModel"
Cohesion: 0.47
Nodes (4): ObservableObject, ObservableCollection, Song, SongListViewModel

### Community 119 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 120 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): AllSongsCompactView, AllSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 121 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): ArtistDetailCompactView, ArtistDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 122 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 123 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): GenreDetailCompactView, GenreDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 124 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 125 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 126 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): ItemClickEventArgs, SelectionChangedEventArgs, YearDetailCompactView, YearDetailListView

### Community 127 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 128 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): MostPlayedSongsCompactView, MostPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 129 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): PlayListSongsCompactView, PlayListSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 130 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyAddedSongsCompactView, RecentlyAddedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 131 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 132 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyPlayedSongsCompactView, RecentlyPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 135 - "ArtistTileView"
Cohesion: 0.40
Nodes (3): ArtistTileView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 138 - "AppIcon"
Cohesion: 0.19
Nodes (7): AppIcon, AppTitleBar, SongLastPlayed, SongPlayCount, Run, ImageIcon, TitleBar

### Community 139 - "SettingsExpander"
Cohesion: 0.40
Nodes (5): FileExt, LibaryPlayListVisiblity, Scan, TintSettings, SettingsExpander

### Community 140 - "PART_TextBox"
Cohesion: 0.50
Nodes (3): PART_TextBox, ResourceDictionary, TextBox

### Community 141 - "AutoScrollView"
Cohesion: 0.50
Nodes (4): AutoScrollHeader, AutoScrollView, AutoScrollHeader, AutoScrollHeader

## Knowledge Gaps
- **135 isolated node(s):** `ABE`, `ABM`, `ABS`, `QUNS`, `AlbumNameRow` (+130 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **29 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Tunetastic.Common.Services.TaskbarOverlay` connect `ShellNotificationWindow` to `MusicControlViewModel.cs`, `TaskbarOverlayManager`, `TaskbarInfo`, `FullscreenStateService`?**
  _High betweenness centrality (0.144) - this node is a cross-community bridge._
- **Why does `Tunetastic.Common.Services` connect `PlaybackTracker` to `GlobalUsings.cs`, `PlaybackStateChangedArgs`?**
  _High betweenness centrality (0.123) - this node is a cross-community bridge._
- **Why does `Tunetastic.Views` connect `Tunetastic.Views` to `GlobalUsings.cs`, `ShellNotificationWindow`?**
  _High betweenness centrality (0.114) - this node is a cross-community bridge._
- **What connects `ABE`, `ABM`, `ABS` to the rest of the system?**
  _135 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `NativeMethods` be split into smaller, more focused modules?**
  _Cohesion score 0.08065458796025717 - nodes in this community are weakly interconnected._
- **Should `TextBlock` be split into smaller, more focused modules?**
  _Cohesion score 0.051203277009728626 - nodes in this community are weakly interconnected._
- **Should `TileListPageBase` be split into smaller, more focused modules?**
  _Cohesion score 0.056107539450613676 - nodes in this community are weakly interconnected._