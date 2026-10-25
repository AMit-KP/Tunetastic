# Graph Report - Tunetastic  (2026-10-15)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 3073 nodes · 5969 edges · 185 communities (147 shown, 38 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 290 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `165efd0f`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- AudioService
- TextBlock
- NativeMethods
- Page
- Tunetastic
- TaskbarOverlayWindow
- .MakeNextButton
- Models.cs
- Page
- TaskbarOverlayManager
- AlbumsViewPage
- render.py
- MainPage
- AlbumDetailPage
- AnimatedScanResultLabel
- MainPlayerPage
- ArtistsViewPage
- SettingsPage
- AllSongsViewPage
- App
- LibraryWatcherService
- .ScrollToSong
- InlineSuggestBox
- Tunetastic.Overlay.Layouts
- ToggleSwitch
- AlphabetScrubber
- SongListPageBase
- RoutedEventArgs
- SongListPageBase
- SongListPageBase
- SongListPageBase
- RecentlyAdded
- Type
- Task
- SongListPageBase
- SongListPageBase
- SongListPageBase
- SongListPageBase
- StackPanel
- SmoothProgressBar
- YearDetailPage
- TunetasticPageBase
- AlbumTintOverlay
- Rectangle
- RoutedEventArgs
- Page
- GenreDetailPage
- GenresViewPage
- PlayListTemplate
- RecentlyPlayed
- .RunCatchUpDiff
- List
- TileListPageBase
- TileListPageBase
- ArtistDetailPage
- MostPlayed
- DropDownButton
- MusicPlayer
- ImportExportPlaylist
- DatabaseHelper
- TaskbarLayoutService
- FullscreenStateService
- ContentDialog
- SongListPageBase
- TaskbarInfo
- AlbumTintProgressOverlay
- RadioMenuFlyoutItem
- TileListPageBase
- TileListPageBase
- MainWindow
- MenuFlyout
- RoutedEventArgs
- Tunetastic.Views.Common
- TileListPageBase
- VisualState
- YearsViewPage
- RoutedEventArgs
- LrcParser
- ListView
- AccentAncientScrollOverlay
- ColorHelper
- Button
- WindowsMediaBackend
- .Search
- SongListViewModel
- MenuFlyoutSubItem
- .GetSuggestions
- DiskSpeedDetector
- .Info
- FlyleafMediaBackend
- SmartWrapVirtualizingLayout
- PlaybackTracker
- MusicControlViewModel.cs
- .EditInfoSaveButtonEnableUpdate
- RoutedEventArgs
- .ReloadArtistSplitRules
- .DetectRenamesAndMoves
- TopAlbumAccentStripeOverlay
- .Create
- OverlayGridCreation
- .AnimateScramble
- .UpdateListBasedOnSorting
- IMediaBackend
- .LoadSong
- .ExtensionConfirmButton_Click
- RoutedEventArgs
- GlobalUsings.cs
- .ResumeIfEnabled
- Song
- Tunetastic.Views
- MusicControl
- RelayCommand
- .TileView_SelectionChanged
- AlphabetScrubber
- .UpdateListBasedOnSorting
- .UpdateListBasedOnSorting
- .PlayAllButton_OnClick
- .UpdateListBasedOnSorting
- .ScrollToTile
- .OnLibraryReadyAsync
- .Success
- .BuildFileScanMeta
- .ProcessPendingTagWritesAsync
- ShellNotificationWindow
- OverlayBase
- TogglePlayPause
- .ListView_SelectionChanged
- .ApplyAndSaveTint
- ToggleButton
- .Page_Loaded
- .UpdateListBasedOnViewStyle
- Tunetastic.Common.Controls
- .OnColorChanged
- HighlightExtensions
- PlaybackStateChangedArgs
- .Album_Tapped
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ApplyOffsetLive
- RecentlyPlayedPage
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .UpdateShimmerPlaceholderCount
- .CreateThumbnailImage
- TextOnlyReversedOverlay
- .Artist_Tapped
- .SortButton_OnClick
- .Genre_Tapped
- SongLastPlayed
- .ListView_SelectionChanged
- .Page_SizeChanged
- .Page_SizeChanged
- collect_github_traffic.py
- PART_TextBox
- AutoScrollView
- .CreateFreshPage
- QueuedList
- AppData.json
- MusicControlsArea
- .ViewButton_OnClick
- .ViewButton_OnClick
- .TimeStyle_Click
- Converters.xaml
- Fonts.xaml
- ThemeResources.xaml
- .OnNavigatingFrom
- .CreateFreshPage
- .Page_SizeChanged
- .OnNavigatingFrom
- .OnNavigatedTo
- .CreateFreshPage
- AppTitle
- VolumeButtonGlyph

## God Nodes (most connected - your core abstractions)
1. `DatabaseHelper` - 99 edges
2. `TextBlock` - 86 edges
3. `Page` - 79 edges
4. `RadioMenuFlyoutItem` - 78 edges
5. `SettingsPage` - 77 edges
6. `Page` - 77 edges
7. `StackPanel` - 71 edges
8. `MainPage` - 66 edges
9. `NativeMethods` - 57 edges
10. `MainPlayerPage` - 56 edges

## Surprising Connections (you probably didn't know these)
- `CountsDot` --references--> `TextBlock`  [EXTRACTED]
  Common/Controls/AnimatedScanResultLabel.xaml → Views/MainPage.xaml
- `TimeDot` --references--> `TextBlock`  [EXTRACTED]
  Common/Controls/AnimatedScanResultLabel.xaml → Views/MainPage.xaml
- `App` --inherits--> `Application`  [EXTRACTED]
  App.xaml.cs → App.xaml
- `TaskbarOverlayWindow` --inherits--> `WindowEx`  [EXTRACTED]
  Common/Services/TaskbarOverlay/TaskbarOverlayWindow.cs → MainWindow.xaml
- `FoldersStat` --references--> `StackPanel`  [EXTRACTED]
  Common/Controls/AnimatedScanResultLabel.xaml → Views/MainPage.xaml

## Import Cycles
- None detected.

## Communities (185 total, 38 thin omitted)

### Community 0 - "AudioService"
Cohesion: 0.05
Nodes (26): AudioSessionControl, AudioSessionDisconnectReason, AudioSessionState, AudioVolumeNotificationData, CancellationTokenSource, bool, double, Func (+18 more)

### Community 1 - "TextBlock"
Cohesion: 0.06
Nodes (56): Key, Value, AddPlaylistDialogDescription, AlbumChanged, AlbumGrid, AlbumTeachingTip, AlbumTeachingTipContent, AppIcon (+48 more)

### Community 2 - "NativeMethods"
Cohesion: 0.09
Nodes (26): APPBARDATA, int, IntPtr, string, ABE, ABM, ABS, APPBARDATA (+18 more)

### Community 3 - "Page"
Cohesion: 0.05
Nodes (50): ActualHeight, All, FontSize, VolumeSlider, Slider, AboutTextBlock, AppearanceTextBlock, AudioTextBlock (+42 more)

### Community 4 - "Tunetastic"
Cohesion: 0.04
Nodes (52): net9.0-windows10.0.26100.0, CommunityToolkit.Common (8.4.2), CommunityToolkit.HighPerformance (8.4.2), CommunityToolkit.Labs.WinUI.Controls.DataTable (0.1.251217-build.2433), CommunityToolkit.Labs.WinUI.Shimmer (0.1.250811-build.2202), CommunityToolkit.Mvvm (8.4.2), CommunityToolkit.WinUI.Animations (8.2.251219), CommunityToolkit.WinUI.Behaviors (8.2.251219) (+44 more)

### Community 5 - "TaskbarOverlayWindow"
Cohesion: 0.07
Nodes (16): OverlayRect, bool, DispatcherTimer, Grid, int, IntPtr, PointerRoutedEventArgs, SUBCLASSPROC (+8 more)

### Community 6 - ".MakeNextButton"
Cohesion: 0.13
Nodes (32): Grid, Grid, Build(), Grid, Build(), Grid, Build(), Grid (+24 more)

### Community 7 - "Models.cs"
Cohesion: 0.05
Nodes (41): string, Constants, ArtistRuleType, BackendType, DiskKind, FadeType, FileChangeType, LocalSave (+33 more)

### Community 8 - "Page"
Cohesion: 0.06
Nodes (41): Album, Artist, BackgroundImage, BlurBorder, BlurEffect, CoverArt, CoverArtImage, CoverArtProjection (+33 more)

### Community 9 - "TaskbarOverlayManager"
Cohesion: 0.08
Nodes (16): bool, Brush, Dictionary, DispatcherQueue, DispatcherTimer, int, IntPtr, IReadOnlyList (+8 more)

### Community 10 - "AlbumsViewPage"
Cohesion: 0.12
Nodes (12): bool, Button, ContainerContentChangingEventArgs, double, int, List, ListViewBase, NavigatingCancelEventArgs (+4 more)

### Community 11 - "render.py"
Cohesion: 0.10
Nodes (38): axis(), card(), flags_svg(), fmt_date(), grid_and_months(), installs_card(), line_panel(), map_card() (+30 more)

### Community 12 - "MainPage"
Cohesion: 0.08
Nodes (13): NavigationView, NavigationViewItemInvokedEventArgs, NavigationViewSelectionChangedEventArgs, RectInt32, ArtistSplitRule, bool, FrameworkElement, int (+5 more)

### Community 13 - "AlbumDetailPage"
Cohesion: 0.12
Nodes (9): MultiSelectButton, Button, ListView, NavigatingCancelEventArgs, ObservableCollection, Page, RoutedEventArgs, Song (+1 more)

### Community 14 - "AnimatedScanResultLabel"
Cohesion: 0.11
Nodes (17): bool, Dictionary, DispatcherTimer, double, int, List, long, RoutedEventArgs (+9 more)

### Community 15 - "MainPlayerPage"
Cohesion: 0.08
Nodes (18): Compositor, MusicPlayer, CloseLyricsButton, SyncLyricsButton, BitmapImage, bool, Button, DispatcherQueue (+10 more)

### Community 16 - "ArtistsViewPage"
Cohesion: 0.08
Nodes (17): MultiSelectButton, ArtistModel, bool, Button, ContainerContentChangingEventArgs, double, int, List (+9 more)

### Community 17 - "SettingsPage"
Cohesion: 0.07
Nodes (16): MusicFormatCategoryModel, NumberBox, NumberBoxValueChangedEventArgs, SettingViewModel, {x:Bind Extension}, DependencyPropertyChangedEventArgs, FrameworkElement, int (+8 more)

### Community 18 - "AllSongsViewPage"
Cohesion: 0.10
Nodes (13): MultiSelectButton, PlayAll, Button, List, ListView, NavigationEventArgs, ObservableCollection, Page (+5 more)

### Community 19 - "App"
Cohesion: 0.07
Nodes (21): Application, IntPtr, SystemTrayIcon, App, AudioService, HashSet, Task, PostUpdateScanService (+13 more)

### Community 20 - "LibraryWatcherService"
Cohesion: 0.12
Nodes (15): ConcurrentDictionary, DateTime, Dictionary, FileScanMeta, int, List, Task, Timer (+7 more)

### Community 21 - ".ScrollToSong"
Cohesion: 0.10
Nodes (13): MenuFlyout, SongListViewModel, bool, Button, IOrderedEnumerable, ListView, ListViewBase, RoutedEventArgs (+5 more)

### Community 22 - "InlineSuggestBox"
Cohesion: 0.11
Nodes (17): ArtistSplitRule, bool, DependencyProperty, int, KeyRoutedEventArgs, List, PointerRoutedEventArgs, RoutedEventArgs (+9 more)

### Community 23 - "Tunetastic.Overlay.Layouts"
Cohesion: 0.06
Nodes (20): Tunetastic.Overlay.Layouts, BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage (+12 more)

### Community 24 - "ToggleSwitch"
Cohesion: 0.07
Nodes (22): AlbumsToggle, ArtistsToggle, AutoAdvanceSwitch, AutoStart, AutoSyncSwitch, CheckForUpdatesStartupToggle, GenresToggle, IgnoreDup (+14 more)

### Community 25 - "AlphabetScrubber"
Cohesion: 0.07
Nodes (22): Rail, Root, UserControl, bool, ContainerContentChangingEventArgs, DispatcherQueue, double, HashSet (+14 more)

### Community 26 - "SongListPageBase"
Cohesion: 0.07
Nodes (29): ActualYearGroup, CompactViewStyle, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, Header, ListViewStyle (+21 more)

### Community 27 - "RoutedEventArgs"
Cohesion: 0.09
Nodes (14): AlbumTextBox, ArtistTextBox, BrowseCoverArtButton, ClearButton, GenreAutoSuggestBox, OpenContainingFolderButton, RemoveCoverArtButton, SearchBox (+6 more)

### Community 28 - "SongListPageBase"
Cohesion: 0.10
Nodes (25): ActualAlbumGroup, AlbumDetailCompactViewGrid, AlbumDetailListViewGrid, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+17 more)

### Community 29 - "SongListPageBase"
Cohesion: 0.08
Nodes (24): AllSongsCompactViewGrid, AllSongsListViewGrid, CompactViewStyle, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock (+16 more)

### Community 30 - "SongListPageBase"
Cohesion: 0.10
Nodes (24): ActualArtistGroup, AlbumSort, ArtistDetailCompactViewGrid, ArtistDetailListViewGrid, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+16 more)

### Community 31 - "RecentlyAdded"
Cohesion: 0.13
Nodes (10): RecentlyAddedPage, Button, DispatcherTimer, List, ListView, ObservableCollection, Page, RoutedEventArgs (+2 more)

### Community 32 - "Type"
Cohesion: 0.12
Nodes (12): BitmapImage, ConcurrentDictionary, int, Type, CoverImageConverter, DateFormatConverter, DurationConverter, DurationToFullTimeConverter (+4 more)

### Community 34 - "SongListPageBase"
Cohesion: 0.10
Nodes (25): ActualGenreGroup, AlbumSort, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText, Descending (+17 more)

### Community 35 - "SongListPageBase"
Cohesion: 0.09
Nodes (25): CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200, Limit50 (+17 more)

### Community 36 - "SongListPageBase"
Cohesion: 0.07
Nodes (31): CustomProgressBar, DeleteDialogText, ErrorMessage, ExportErrorMessage, ExportFormat, ExportPlayList, ExportTextBox, GoToSettings (+23 more)

### Community 37 - "SongListPageBase"
Cohesion: 0.10
Nodes (25): CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200 (+17 more)

### Community 38 - "StackPanel"
Cohesion: 0.10
Nodes (25): CountsDot, FoldersStat, FoldersValue, LibrariesStat, LibrariesValue, MessageValue, SongsStat, SongsValue (+17 more)

### Community 39 - "SmoothProgressBar"
Cohesion: 0.13
Nodes (12): bool, Brush, DependencyProperty, DispatcherTimer, double, Grid, PointerRoutedEventArgs, Rectangle (+4 more)

### Community 40 - "YearDetailPage"
Cohesion: 0.09
Nodes (11): Button, ListView, NavigatingCancelEventArgs, NavigationEventArgs, ObservableCollection, Page, RoutedEventArgs, Song (+3 more)

### Community 41 - "TunetasticPageBase"
Cohesion: 0.11
Nodes (14): AlphabetScrubber, Page, ToggleButton, bool, DispatcherQueue, Grid, HashSet, List (+6 more)

### Community 42 - "AlbumTintOverlay"
Cohesion: 0.09
Nodes (19): AccentColorAnalyzer, BaseColorAnalyzer, Border, Button, ColorAnalyzer, ColorWeightAnalyzer, Image, Rectangle (+11 more)

### Community 43 - "Rectangle"
Cohesion: 0.08
Nodes (23): ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill (+15 more)

### Community 44 - "RoutedEventArgs"
Cohesion: 0.08
Nodes (8): CheckForUpdates, ForwardRewindButtonVisibility, MinimizeToTray, PreviousReset, ScanAtStart, VersionInfoToggle, RoutedEventArgs, ProgressButton

### Community 45 - "Page"
Cohesion: 0.10
Nodes (22): DurationConverter, Position, AlbumCover, BottomProgressBar, CurrentDurationConverter, ForwardButton, MusicControls, NextButton (+14 more)

### Community 46 - "GenreDetailPage"
Cohesion: 0.15
Nodes (7): MultiSelectButton, Button, ListView, ObservableCollection, RoutedEventArgs, Song, GenreDetailPage

### Community 47 - "GenresViewPage"
Cohesion: 0.08
Nodes (17): MultiSelectButton, bool, Button, ContainerContentChangingEventArgs, double, GenreModel, int, List (+9 more)

### Community 48 - "PlayListTemplate"
Cohesion: 0.12
Nodes (8): Button, List, NavigationEventArgs, ObservableCollection, Page, Song, TextChangedEventArgs, PlayListTemplate

### Community 49 - "RecentlyPlayed"
Cohesion: 0.13
Nodes (9): Button, DispatcherTimer, List, NavigationEventArgs, ObservableCollection, Page, Song, Task (+1 more)

### Community 50 - ".RunCatchUpDiff"
Cohesion: 0.18
Nodes (11): List, Task, AutoScanReconciler, bool, IEnumerable, List, Song, Task (+3 more)

### Community 51 - "List"
Cohesion: 0.08
Nodes (8): AlbumModel, ArtistModel, ArtistSplitRule, FileScanMeta, GenreModel, List, MusicFormatModel, YearModel

### Community 52 - "TileListPageBase"
Cohesion: 0.09
Nodes (22): AlbumCover, AlbumTextBlock, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, LoadingProgress (+14 more)

### Community 53 - "TileListPageBase"
Cohesion: 0.10
Nodes (20): CommonStates, ArtistTextBlock, Ascending, CommonStates, ContentGrid, CustomProgressBar, DeleteDialogText, Descending (+12 more)

### Community 54 - "ArtistDetailPage"
Cohesion: 0.16
Nodes (6): Button, ListView, ObservableCollection, RoutedEventArgs, Song, ArtistDetailPage

### Community 55 - "MostPlayed"
Cohesion: 0.13
Nodes (8): Button, List, NavigationEventArgs, ObservableCollection, Page, Song, Task, MostPlayed

### Community 56 - "DropDownButton"
Cohesion: 0.10
Nodes (21): SortDropDown, ViewButton, SortDropDown, ViewButton, SortDropDown, ViewButton, SortDropDown, SortDropDown (+13 more)

### Community 57 - "MusicPlayer"
Cohesion: 0.13
Nodes (13): BackendType, bool, IMediaBackend, int, MediaPlayer, Player, RepeatMode, ShuffleMode (+5 more)

### Community 58 - "ImportExportPlaylist"
Cohesion: 0.22
Nodes (6): List, name, Task, ImportExportPlaylist, totalTrackCount, trackInLibrary

### Community 59 - "DatabaseHelper"
Cohesion: 0.13
Nodes (8): HashSet, Regex, AlbumNameRow, ArtistNameRow, DatabaseHelper, SearchCategory, SQLiteAsyncConnection, SQLiteConnection

### Community 60 - "TaskbarLayoutService"
Cohesion: 0.17
Nodes (11): Dictionary, HashSet, int, List, FreeZone, IconUnion, TaskbarAlignment, TaskbarIconLayout (+3 more)

### Community 61 - "FullscreenStateService"
Cohesion: 0.15
Nodes (6): Dictionary, DispatcherTimer, IReadOnlyList, FullscreenStateService, TaskbarStateService, Tunetastic.Common.Services.TaskbarOverlay

### Community 62 - "ContentDialog"
Cohesion: 0.10
Nodes (20): DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog (+12 more)

### Community 63 - "SongListPageBase"
Cohesion: 0.11
Nodes (23): CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettingsTextBlock, Header, Limit100, Limit200, Limit50 (+15 more)

### Community 64 - "TaskbarInfo"
Cohesion: 0.25
Nodes (7): ABE, IntPtr, IReadOnlyList, TaskbarDiscoveryService, TaskbarInfo, IconUnion, RECT

### Community 65 - "AlbumTintProgressOverlay"
Cohesion: 0.12
Nodes (16): AccentColorAnalyzer, BaseColorAnalyzer, Border, Brush, Button, Color, ColorAnalyzer, ColorWeightAnalyzer (+8 more)

### Community 66 - "RadioMenuFlyoutItem"
Cohesion: 0.21
Nodes (15): AlbumSort, ArtistsSort, Ascending, Descending, DurationSort, TitleSort, TrackSort, AlbumSort (+7 more)

### Community 67 - "TileListPageBase"
Cohesion: 0.13
Nodes (16): Ascending, ContentGrid, CustomProgressBar, DeleteDialogText, Descending, GenreTextBlock, GoToSettingsTextBlock, More (+8 more)

### Community 68 - "TileListPageBase"
Cohesion: 0.12
Nodes (18): ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, LoadingProgress, More, MoreButton (+10 more)

### Community 69 - "MainWindow"
Cohesion: 0.14
Nodes (8): AppWindow, WindowEx, SystemTrayIcon, SystemTrayIconEventArgs, void, MainWindow, OverlappedPresenter, WindowEventArgs

### Community 70 - "MenuFlyout"
Cohesion: 0.11
Nodes (18): More, More, More, Sort, Sort, ViewStyle, MenuFlyout, More (+10 more)

### Community 71 - "RoutedEventArgs"
Cohesion: 0.19
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 72 - "Tunetastic.Views.Common"
Cohesion: 0.21
Nodes (3): Tunetastic.Views.PlaylistViews, Tunetastic.Views.LibraryViews, Tunetastic.Views.Common

### Community 73 - "TileListPageBase"
Cohesion: 0.15
Nodes (7): bool, Button, IOrderedEnumerable, ListViewBase, RoutedEventArgs, Task, TileListPageBase

### Community 74 - "VisualState"
Cohesion: 0.12
Nodes (17): Normal, PointerOver, Pressed, Selected, Normal, PointerOver, Pressed, Selected (+9 more)

### Community 75 - "YearsViewPage"
Cohesion: 0.16
Nodes (8): bool, Button, double, int, List, ObservableCollection, RoutedEventArgs, YearsViewPage

### Community 76 - "RoutedEventArgs"
Cohesion: 0.13
Nodes (9): ClearLyricsButton, CopyLyricsButton, EditLyricsButton, OpenLyricsButton, SaveAsOffsetButton, SaveByTimestampsButton, RoutedEventArgs, Task (+1 more)

### Community 77 - "LrcParser"
Cohesion: 0.20
Nodes (7): TimeSpan, LrcLine, List, Regex, LrcParser, Tunetastic.Common.Helpers, Match

### Community 79 - "ListView"
Cohesion: 0.24
Nodes (7): AlbumDetailCompactView, AlbumDetailListView, ItemClickEventArgs, SelectionChangedEventArgs, AlbumTileView, ItemClickEventArgs, ListView

### Community 80 - "AccentAncientScrollOverlay"
Cohesion: 0.17
Nodes (11): await, LinearGradientBrush, AccentColorAnalyzer, Border, Color, ColorAnalyzer, Image, SolidColorBrush (+3 more)

### Community 81 - "ColorHelper"
Cohesion: 0.26
Nodes (6): Color, double, OverlayTheme, ColorHelper, Lab, Lab

### Community 82 - "Button"
Cohesion: 0.25
Nodes (6): AcceptSyncButton, CancelSyncButton, LyricsMenuButton, MusicInfoButton, ShowLyricsButton, Button

### Community 83 - "WindowsMediaBackend"
Cohesion: 0.15
Nodes (7): bool, double, MediaPlayer, Task, Timer, WindowsMediaBackend, MediaPlaybackSession

### Community 84 - ".Search"
Cohesion: 0.20
Nodes (5): IEnumerable, SearchCategory, SearchItem, SearchResults, SearchScope

### Community 85 - "SongListViewModel"
Cohesion: 0.18
Nodes (8): Tunetastic.ViewModels, ObservableObject, RelayCommand, Task, SettingViewModel, ObservableCollection, Song, SongListViewModel

### Community 86 - "MenuFlyoutSubItem"
Cohesion: 0.13
Nodes (15): AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist (+7 more)

### Community 87 - ".GetSuggestions"
Cohesion: 0.19
Nodes (7): AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs, AutoSuggestBoxSuggestionChosenEventArgs, AutoSuggestBoxTextChangedEventArgs, KeyValuePair, SearchItemType, Task

### Community 88 - "DiskSpeedDetector"
Cohesion: 0.32
Nodes (4): Func, TimeSpan, DiskSpeedDetector, DiskKind

### Community 90 - "FlyleafMediaBackend"
Cohesion: 0.14
Nodes (7): bool, long, Player, Task, FlyleafMediaBackend, OpenCompletedArgs, PropertyChangedEventArgs

### Community 91 - "SmartWrapVirtualizingLayout"
Cohesion: 0.24
Nodes (8): double, FrameworkElement, int, Size, LayoutState, SmartWrapVirtualizingLayout, VirtualizingLayout, VirtualizingLayoutContext

### Community 92 - "PlaybackTracker"
Cohesion: 0.17
Nodes (4): DateTime, TimeSpan, PlaybackTracker, Tunetastic.Common.Services

### Community 93 - "MusicControlViewModel.cs"
Cohesion: 0.18
Nodes (6): PlaybackStateChangedArgs, Storyboard, OnPlaybackStateChanged(), StartRainbow(), StopRainbow(), UpdateStoryBoard()

### Community 94 - ".EditInfoSaveButtonEnableUpdate"
Cohesion: 0.24
Nodes (6): LyricsTextBox, PlaylistNameBox, TitleTextBox, TextChangedEventArgs, TextBox, YearNumberBox

### Community 95 - "RoutedEventArgs"
Cohesion: 0.19
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 97 - ".DetectRenamesAndMoves"
Cohesion: 0.18
Nodes (10): Dictionary, FileScanMeta, List, RenameDetector, RenameMatchResult, CreationTimeUtc, FileSizeBytes, LastModifiedUtc (+2 more)

### Community 98 - "TopAlbumAccentStripeOverlay"
Cohesion: 0.17
Nodes (11): AccentColorAnalyzer, BaseColorAnalyzer, Border, ColorAnalyzer, ColorWeightAnalyzer, double, Image, Rectangle (+3 more)

### Community 99 - ".Create"
Cohesion: 0.18
Nodes (7): Tunetastic.Overlay, OverlayLayout, OverlayTheme, OverlayFactory, IReadOnlyList, OverlayLayoutCatalog, OverlayLayoutInfo

### Community 100 - "OverlayGridCreation"
Cohesion: 0.24
Nodes (11): ProcessKeyboardAcceleratorEventArgs, UISettings, OverlayTheme, UIElement, keyboardInput(), NextSong(), OverlayGridCreation(), PreviousSong() (+3 more)

### Community 101 - ".AnimateScramble"
Cohesion: 0.33
Nodes (4): Grid, StackPanel, RollingColumn, TranslateTransform

### Community 102 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 103 - "IMediaBackend"
Cohesion: 0.20
Nodes (4): Task, IMediaBackend, Tunetastic.Common.Services.Backends, IDisposable

### Community 106 - ".ExtensionConfirmButton_Click"
Cohesion: 0.20
Nodes (6): BuyMeACoffee, ExtensionConfirmButton, FullScanButton, GitHubIssues, RateThisAppButton, Button

### Community 112 - "Tunetastic.Views"
Cohesion: 0.22
Nodes (3): Tunetastic.Views, Task, SplashScreen

### Community 113 - "MusicControl"
Cohesion: 0.31
Nodes (3): MusicControlViewModel, Storyboard, MusicControl

### Community 114 - "RelayCommand"
Cohesion: 0.25
Nodes (9): RelayCommand, RepeatMode, ShuffleMode, ForwardSong(), _musicPlayer_ShuffleStatusChanged(), RepeatButtonToggle(), RewindSong(), SetShuffleAndRepeat() (+1 more)

### Community 115 - ".TileView_SelectionChanged"
Cohesion: 0.14
Nodes (8): SelectionChangedEventArgs, SelectionChangedEventArgs, ArtistTileView, ItemClickEventArgs, SelectionChangedEventArgs, GenreTileView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 116 - "AlphabetScrubber"
Cohesion: 0.22
Nodes (9): AlphabetScrubber, AlphabetScrubber, AlphabetScrubber, AlphabetScrubber, AlphabetScrubber, AlphabetScrubber, AlphabetScrubber, AlphabetScrubber (+1 more)

### Community 117 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 118 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 119 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 121 - ".ScrollToTile"
Cohesion: 0.28
Nodes (4): Ascending, Descending, Task, YearModel

### Community 122 - ".OnLibraryReadyAsync"
Cohesion: 0.16
Nodes (5): CompactViewStyle, DateStyle, ListViewStyle, RelativeTimeStyle, Task

### Community 124 - ".BuildFileScanMeta"
Cohesion: 0.46
Nodes (4): Task, FileChangeProcessor, FileScanMeta, FileChangeType

### Community 125 - ".ProcessPendingTagWritesAsync"
Cohesion: 0.36
Nodes (3): List, Song, Task

### Community 126 - "ShellNotificationWindow"
Cohesion: 0.32
Nodes (5): bool, Dictionary, DispatcherQueue, ShellNotificationWindow, WndProcDelegate

### Community 127 - "OverlayBase"
Cohesion: 0.25
Nodes (5): bool, Grid, OverlayTheme, UIElement, OverlayBase

### Community 128 - "TogglePlayPause"
Cohesion: 0.25
Nodes (8): KeyRoutedEventArgs, SystemTrayIcon, SystemTrayIconEventArgs, Task, LoadLastPlayedTrack(), OnTrayIconLeftClick(), PreviewKeyDownMusicControl(), TogglePlayPause()

### Community 129 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): AllSongsCompactView, AllSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 130 - ".ApplyAndSaveTint"
Cohesion: 0.29
Nodes (4): ColorChangedEventArgs, ColorPaletteColorChangedEventArgs, ColorPicker, Color

### Community 131 - "ToggleButton"
Cohesion: 0.29
Nodes (6): MultiSelectButton, MultiSelectButton, MultiSelectButton, ShuffleButton, ToggleButton, MultiSelectButton

### Community 132 - ".Page_Loaded"
Cohesion: 0.29
Nodes (3): ContainerContentChangingEventArgs, ListViewBase, SizeChangedEventArgs

### Community 133 - ".UpdateListBasedOnViewStyle"
Cohesion: 0.33
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 136 - ".OnColorChanged"
Cohesion: 0.33
Nodes (3): DependencyPropertyChangedEventArgs, SolidColorBrush, DependencyObject

### Community 137 - "HighlightExtensions"
Cohesion: 0.47
Nodes (3): Color, FrameworkElement, HighlightExtensions

### Community 138 - "PlaybackStateChangedArgs"
Cohesion: 0.33
Nodes (3): PlaybackStateChangedArgs, EventArgs, PlaybackState

### Community 140 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): ArtistDetailCompactView, ArtistDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 141 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 142 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): GenreDetailCompactView, GenreDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 143 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 146 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 147 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): MostPlayedSongsCompactView, MostPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 148 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): PlayListSongsCompactView, PlayListSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 149 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 150 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyAddedSongsCompactView, RecentlyAddedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 151 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 152 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyPlayedSongsCompactView, RecentlyPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 154 - ".CreateThumbnailImage"
Cohesion: 0.40
Nodes (3): ImageResizer, IPicture, ThumbnailFolder

### Community 159 - "SongLastPlayed"
Cohesion: 0.67
Nodes (3): SongLastPlayed, SongPlayCount, Run

### Community 160 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (3): ItemClickEventArgs, SelectionChangedEventArgs, YearTileView

### Community 163 - "collect_github_traffic.py"
Cohesion: 0.83
Nodes (3): blank(), get(), main()

### Community 164 - "PART_TextBox"
Cohesion: 0.50
Nodes (3): PART_TextBox, ResourceDictionary, TextBox

### Community 165 - "AutoScrollView"
Cohesion: 0.50
Nodes (4): AutoScrollHeader, AutoScrollView, AutoScrollHeader, AutoScrollHeader

### Community 171 - "MusicControlsArea"
Cohesion: 0.67
Nodes (3): MusicControlsArea, NavFrame, Frame

## Knowledge Gaps
- **136 isolated node(s):** `ABE`, `ABM`, `ABS`, `QUNS`, `AlbumNameRow` (+131 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **38 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Tunetastic.Common.Services.TaskbarOverlay` connect `FullscreenStateService` to `Tunetastic.Views`, `TaskbarOverlayManager`, `TaskbarLayoutService`, `MusicControlViewModel.cs`?**
  _High betweenness centrality (0.148) - this node is a cross-community bridge._
- **Why does `SettingsPage` connect `SettingsPage` to `.ApplyAndSaveTint`, `Page`, `TunetasticPageBase`, `.ExtensionConfirmButton_Click`, `TaskbarOverlayManager`, `RoutedEventArgs`, `.ResumeIfEnabled`, `Tunetastic.Views`, `ToggleSwitch`?**
  _High betweenness centrality (0.127) - this node is a cross-community bridge._
- **Why does `Tunetastic.Common.Services` connect `PlaybackTracker` to `PlaybackStateChangedArgs`, `App`, `GlobalUsings.cs`, `.ResumeIfEnabled`?**
  _High betweenness centrality (0.115) - this node is a cross-community bridge._
- **What connects `ABE`, `ABM`, `ABS` to the rest of the system?**
  _136 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `AudioService` be split into smaller, more focused modules?**
  _Cohesion score 0.05472636815920398 - nodes in this community are weakly interconnected._
- **Should `TextBlock` be split into smaller, more focused modules?**
  _Cohesion score 0.05827067669172932 - nodes in this community are weakly interconnected._
- **Should `NativeMethods` be split into smaller, more focused modules?**
  _Cohesion score 0.08754208754208755 - nodes in this community are weakly interconnected._