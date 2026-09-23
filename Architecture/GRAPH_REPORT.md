# Graph Report - Tunetastic  (2026-09-21)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 2923 nodes · 5641 edges · 173 communities (147 shown, 26 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 259 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f8f0ab7e`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- MusicControlViewModel.cs
- AudioService
- TextBlock
- Tunetastic
- NativeMethods
- InlineSuggestBox
- ColorHelper
- Page
- DatabaseHelper
- MainPlayerPage
- Page
- TaskbarOverlayManager
- TaskbarInfo
- Models.cs
- OverlayBase
- MainPage
- SettingsPage
- AnimatedScanResultLabel
- TaskbarOverlayWindow
- AlbumDetailPage
- AllSongsViewPage
- AlbumsViewPage
- Tunetastic.Overlay.Layouts
- Page
- SmoothProgressBar
- RadioMenuFlyoutItem
- .ScrollToSong
- RoutedEventArgs
- SongListPageBase
- Rectangle
- TileListPageBase
- RecentlyPlayed
- App
- StackPanel
- SongListPageBase
- SongListPageBase
- SongListPageBase
- SongListPageBase
- List
- TileListPageBase
- SongListPageBase
- SongListPageBase
- YearsViewPage
- LibraryWatcherService
- SongListPageBase
- RecentlyAdded
- FullscreenStateService
- TopAlbumAccentStripeOverlay
- ArtistsViewPage
- GenresViewPage
- RoutedEventArgs
- .ExportPlayList_Click
- ArtistDetailPage
- .Genre_Tapped
- YearDetailPage
- MostPlayed
- .RunCatchUpDiff
- DropDownButton
- MusicPlayer
- Button
- ImportExportPlaylist
- ContentDialog
- TileListPageBase
- TileListPageBase
- ShellNotificationWindow
- AlbumTintProgressOverlay
- MainWindow
- Type
- TunetasticPageBase
- MenuFlyout
- Tunetastic.Views.Common
- SettingViewModel
- VisualState
- AccentAncientScrollOverlay
- LrcParser
- WindowsMediaBackend
- .Search
- TileListPageBase
- MenuFlyoutSubItem
- PlayListTemplate
- .GetSuggestions
- GlobalUsings.cs
- DiskSpeedDetector
- FlyleafMediaBackend
- .SyncSongArtistsForSong
- Build
- .EditInfoSaveButtonEnableUpdate
- .Info
- .DetectRenamesAndMoves
- .ResumeIfEnabled
- RoutedEventArgs
- GenreDetailPage
- RoutedEventArgs
- Tunetastic.Common.Services
- .Create
- RoutedEventArgs
- RoutedEventArgs
- .UpdateListBasedOnMaxLimit
- IMediaBackend
- .LoadSong
- .ListView_SelectionChanged
- ComboBox
- Song
- .TileView_SelectionChanged
- .UpdateListBasedOnSorting
- Page
- OverlayGridCreation
- .UpdateListBasedOnSorting
- .UpdateListBasedOnSorting
- .UpdateListBasedOnSorting
- .OnLibraryReadyAsync
- .BuildFileScanMeta
- .ProcessPendingTagWritesAsync
- PlaybackTracker
- RoutedEventArgs
- Button
- .ApplyAndSaveTint
- AlbumTintOverlay
- Slider
- .UpdateListBasedOnViewStyle
- .PlayAllButton_OnClick
- .ReloadArtistSplitRules
- Build
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- ToggleButton
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- ListView
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- SettingsCard
- VisualStateGroup
- MusicControl
- GenreTileView
- RepeatButton
- SongListViewModel
- .TimeStyle_Click
- PART_TextBox
- AutoScrollView
- QueuedList
- TogglePlayPause
- AppData.json
- .NumberBox_ValueChanged
- CoverArtImage
- MusicControlsArea
- .Page_Loaded
- CheckForUpdates
- Converters.xaml
- Fonts.xaml
- ThemeResources.xaml
- AppTitle
- NavView
- IgnoretracksDuration
- .Ext_ToggleSwitch_OnToggled
- .SyncLyricsButton_Click
- .ListView_SelectionChanged
- TextOnlyReversedOverlay
- CurrentDurationConverter
- .Page_ActualThemeChanged
- .CreateFreshPage
- .Page_SizeChanged
- .OnNavigatedTo
- .CreateFreshPage

## God Nodes (most connected - your core abstractions)
1. `DatabaseHelper` - 96 edges
2. `TextBlock` - 85 edges
3. `StackPanel` - 79 edges
4. `Page` - 77 edges
5. `RadioMenuFlyoutItem` - 73 edges
6. `Page` - 70 edges
7. `SettingsPage` - 66 edges
8. `MainPage` - 65 edges
9. `NativeMethods` - 57 edges
10. `MainPlayerPage` - 56 edges

## Surprising Connections (you probably didn't know these)
- `CountsDot` --references--> `TextBlock`  [EXTRACTED]
  Common/Controls/AnimatedScanResultLabel.xaml → Views/MainPage.xaml
- `TimeDot` --references--> `TextBlock`  [EXTRACTED]
  Common/Controls/AnimatedScanResultLabel.xaml → Views/MainPage.xaml
- `TaskbarOverlayWindow` --inherits--> `WindowEx`  [EXTRACTED]
  Common/Services/TaskbarOverlay/TaskbarOverlayWindow.cs → MainWindow.xaml
- `App` --inherits--> `Application`  [EXTRACTED]
  App.xaml.cs → App.xaml
- `FoldersStat` --references--> `StackPanel`  [EXTRACTED]
  Common/Controls/AnimatedScanResultLabel.xaml → Views/MainPage.xaml

## Import Cycles
- None detected.

## Communities (173 total, 26 thin omitted)

### Community 0 - "MusicControlViewModel.cs"
Cohesion: 0.13
Nodes (15): PlaybackStateChangedArgs, RelayCommand, RepeatMode, ShuffleMode, Storyboard, ForwardSong(), _musicPlayer_ShuffleStatusChanged(), OnPlaybackStateChanged() (+7 more)

### Community 1 - "AudioService"
Cohesion: 0.05
Nodes (25): AudioSessionControl, AudioSessionDisconnectReason, AudioSessionState, AudioVolumeNotificationData, bool, IntPtr, List, Name (+17 more)

### Community 2 - "TextBlock"
Cohesion: 0.06
Nodes (56): Key, Value, AddPlaylistDialogDescription, AlbumChanged, AlbumGrid, AlbumTeachingTip, AlbumTeachingTipContent, AppIcon (+48 more)

### Community 3 - "Tunetastic"
Cohesion: 0.04
Nodes (55): net9.0-windows10.0.26100.0, CommunityToolkit.Common (8.4.2), CommunityToolkit.HighPerformance (8.4.2), CommunityToolkit.Labs.WinUI.Controls.DataTable (0.1.251217-build.2433), CommunityToolkit.Labs.WinUI.Shimmer (0.1.250811-build.2202), CommunityToolkit.Mvvm (8.4.2), CommunityToolkit.WinUI.Animations (8.2.251219), CommunityToolkit.WinUI.Behaviors (8.2.251219) (+47 more)

### Community 4 - "NativeMethods"
Cohesion: 0.10
Nodes (15): APPBARDATA, IntPtr, ABE, ABM, ABS, NativeMethods, QUNS, DllImport (+7 more)

### Community 5 - "InlineSuggestBox"
Cohesion: 0.06
Nodes (26): ArtistSplitRule, bool, DependencyProperty, int, KeyRoutedEventArgs, List, PointerRoutedEventArgs, RoutedEventArgs (+18 more)

### Community 6 - "ColorHelper"
Cohesion: 0.08
Nodes (18): AppConfig, AppHelper, Color, double, OverlayTheme, ColorHelper, Lab, Color (+10 more)

### Community 7 - "Page"
Cohesion: 0.07
Nodes (39): FontSize, AboutTextBlock, AlbumsToggle, AppearanceTextBlock, ArtistsToggle, AudioTextBlock, AutoAdvanceSwitch, AutoStart (+31 more)

### Community 8 - "DatabaseHelper"
Cohesion: 0.09
Nodes (9): HashSet, LibraryModel, Regex, Task, AlbumNameRow, ArtistNameRow, DatabaseHelper, SearchCategory (+1 more)

### Community 9 - "MainPlayerPage"
Cohesion: 0.09
Nodes (16): Compositor, MusicPlayer, BitmapImage, bool, Button, DispatcherQueue, DispatcherTimer, double (+8 more)

### Community 10 - "Page"
Cohesion: 0.06
Nodes (41): Album, Artist, BackgroundImage, BlurBorder, BlurEffect, CoverArt, CoverArtImage, CoverArtProjection (+33 more)

### Community 11 - "TaskbarOverlayManager"
Cohesion: 0.08
Nodes (16): bool, Brush, Dictionary, DispatcherQueue, DispatcherTimer, int, IntPtr, IReadOnlyList (+8 more)

### Community 12 - "TaskbarInfo"
Cohesion: 0.12
Nodes (18): ABE, Dictionary, HashSet, int, IntPtr, IReadOnlyList, List, FreeZone (+10 more)

### Community 13 - "Models.cs"
Cohesion: 0.06
Nodes (36): string, Constants, ArtistRuleType, BackendType, DiskKind, FadeType, FileChangeType, LocalSave (+28 more)

### Community 14 - "OverlayBase"
Cohesion: 0.12
Nodes (32): Grid, Grid, Build(), Grid, Build(), Grid, Build(), Grid (+24 more)

### Community 15 - "MainPage"
Cohesion: 0.08
Nodes (13): NavigationView, NavigationViewItemInvokedEventArgs, NavigationViewSelectionChangedEventArgs, RectInt32, ArtistSplitRule, bool, FrameworkElement, int (+5 more)

### Community 16 - "SettingsPage"
Cohesion: 0.09
Nodes (11): SettingViewModel, DependencyPropertyChangedEventArgs, FrameworkElement, LibraryModel, MusicFormatModel, ObservableCollection, OverlayLayout, RangeBaseValueChangedEventArgs (+3 more)

### Community 17 - "AnimatedScanResultLabel"
Cohesion: 0.10
Nodes (21): bool, Dictionary, DispatcherTimer, double, Grid, int, List, long (+13 more)

### Community 18 - "TaskbarOverlayWindow"
Cohesion: 0.08
Nodes (15): OverlayRect, bool, DispatcherTimer, Grid, int, IntPtr, PointerRoutedEventArgs, SUBCLASSPROC (+7 more)

### Community 19 - "AlbumDetailPage"
Cohesion: 0.08
Nodes (15): CompactViewStyle, ListViewStyle, MultiSelectButton, Button, FrameworkElement, ListView, NavigatingCancelEventArgs, NavigationEventArgs (+7 more)

### Community 20 - "AllSongsViewPage"
Cohesion: 0.08
Nodes (17): MoreButton, MultiSelectButton, PlayAll, ShuffleAndPlay, Button, FrameworkElement, List, ListView (+9 more)

### Community 21 - "AlbumsViewPage"
Cohesion: 0.09
Nodes (17): AlbumModel, bool, Button, ContainerContentChangingEventArgs, double, FrameworkElement, int, List (+9 more)

### Community 22 - "Tunetastic.Overlay.Layouts"
Cohesion: 0.07
Nodes (15): Tunetastic.Overlay.Layouts, BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage (+7 more)

### Community 23 - "Page"
Cohesion: 0.14
Nodes (16): AlbumCover, BottomProgressBar, MusicControls, NextButton, Page, PlayPauseButton, PrevButton, RepeatButton (+8 more)

### Community 24 - "SmoothProgressBar"
Cohesion: 0.11
Nodes (14): bool, Brush, DependencyProperty, DependencyPropertyChangedEventArgs, DispatcherTimer, double, Grid, PointerRoutedEventArgs (+6 more)

### Community 25 - "RadioMenuFlyoutItem"
Cohesion: 0.09
Nodes (29): ArtistsSort, Ascending, Descending, DurationSort, TitleSort, AlbumSort, AllSongsCompactViewGrid, AllSongsListViewGrid (+21 more)

### Community 26 - ".ScrollToSong"
Cohesion: 0.11
Nodes (11): MenuFlyout, SongListViewModel, Button, FrameworkElement, IOrderedEnumerable, ListView, RoutedEventArgs, SelectionChangedEventArgs (+3 more)

### Community 27 - "RoutedEventArgs"
Cohesion: 0.09
Nodes (14): AlbumTextBox, ArtistTextBox, BrowseCoverArtButton, ClearButton, GenreAutoSuggestBox, OpenContainingFolderButton, RemoveCoverArtButton, SearchBox (+6 more)

### Community 28 - "SongListPageBase"
Cohesion: 0.09
Nodes (23): ActualAlbumGroup, AlbumDetailCompactView, AlbumDetailCompactViewGrid, AlbumDetailListView, AlbumDetailListViewGrid, ContentGrid, CustomProgressBar, DeleteDialogText (+15 more)

### Community 29 - "Rectangle"
Cohesion: 0.12
Nodes (16): ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill (+8 more)

### Community 30 - "TileListPageBase"
Cohesion: 0.10
Nodes (20): Ascending, ContentGrid, CustomProgressBar, DeleteDialogText, Descending, GoToSettings, GoToSettingsTextBlock, LoadingProgress (+12 more)

### Community 31 - "RecentlyPlayed"
Cohesion: 0.11
Nodes (11): RecentlyPlayedPage, Button, DispatcherTimer, List, ListView, NavigationEventArgs, ObservableCollection, Page (+3 more)

### Community 32 - "App"
Cohesion: 0.08
Nodes (17): Application, IntPtr, SystemTrayIcon, App, AudioService, Tunetastic, IJsonNavigationService, IRainbowFrame (+9 more)

### Community 33 - "StackPanel"
Cohesion: 0.10
Nodes (25): CountsDot, FoldersStat, FoldersValue, LibrariesStat, LibrariesValue, MessageValue, SongsStat, SongsValue (+17 more)

### Community 34 - "SongListPageBase"
Cohesion: 0.09
Nodes (25): CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200, Limit50 (+17 more)

### Community 35 - "SongListPageBase"
Cohesion: 0.09
Nodes (25): CustomProgressBar, DeleteDialogText, ErrorMessage, ExportErrorMessage, ExportFormat, ExportTextBox, GoToSettings, GoToSettingsTextBlock (+17 more)

### Community 36 - "SongListPageBase"
Cohesion: 0.09
Nodes (28): CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200 (+20 more)

### Community 37 - "SongListPageBase"
Cohesion: 0.10
Nodes (25): CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200 (+17 more)

### Community 38 - "List"
Cohesion: 0.08
Nodes (8): AlbumModel, ArtistModel, ArtistSplitRule, FileScanMeta, GenreModel, List, MusicFormatModel, YearModel

### Community 39 - "TileListPageBase"
Cohesion: 0.09
Nodes (24): AlbumCover, AlbumTextBlock, AlphabetNavigationPanel, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText, Descending (+16 more)

### Community 40 - "SongListPageBase"
Cohesion: 0.10
Nodes (24): ActualGenreGroup, AlbumSort, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+16 more)

### Community 41 - "SongListPageBase"
Cohesion: 0.10
Nodes (24): ActualYearGroup, AlbumSort, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+16 more)

### Community 42 - "YearsViewPage"
Cohesion: 0.09
Nodes (14): bool, Button, ContainerContentChangingEventArgs, List, ListViewBase, NavigatingCancelEventArgs, NavigationEventArgs, ObservableCollection (+6 more)

### Community 43 - "LibraryWatcherService"
Cohesion: 0.13
Nodes (15): DateTime, Dictionary, HashSet, int, List, object, Timer, TimeSpan (+7 more)

### Community 44 - "SongListPageBase"
Cohesion: 0.10
Nodes (23): ActualArtistGroup, AlbumSort, AlphabetNavigationPanel, ArtistDetailCompactViewGrid, ArtistDetailListViewGrid, Ascending, ContentGrid, CustomProgressBar (+15 more)

### Community 45 - "RecentlyAdded"
Cohesion: 0.10
Nodes (14): DateStyle, MultiSelectButton, RecentlyAddedPage, RelativeTimeStyle, Button, DispatcherTimer, List, ListView (+6 more)

### Community 46 - "FullscreenStateService"
Cohesion: 0.12
Nodes (7): Dictionary, DispatcherTimer, IReadOnlyList, FullscreenStateService, TaskbarStateService, Tunetastic.Common.Services.TaskbarOverlay, Tunetastic.Views

### Community 47 - "TopAlbumAccentStripeOverlay"
Cohesion: 0.17
Nodes (11): AccentColorAnalyzer, BaseColorAnalyzer, Border, ColorAnalyzer, ColorWeightAnalyzer, double, Image, Rectangle (+3 more)

### Community 48 - "ArtistsViewPage"
Cohesion: 0.08
Nodes (15): ArtistModel, bool, Button, ContainerContentChangingEventArgs, FrameworkElement, List, ListViewBase, NavigatingCancelEventArgs (+7 more)

### Community 49 - "GenresViewPage"
Cohesion: 0.12
Nodes (9): bool, Button, FrameworkElement, List, NavigatingCancelEventArgs, NavigationEventArgs, ObservableCollection, Page (+1 more)

### Community 50 - "RoutedEventArgs"
Cohesion: 0.08
Nodes (7): CheckForUpdatesStartupToggle, ForwardRewindButtonVisibility, IgnoreDup, RestartTrackOnSelection, TaskBarOverlay, UseSystemVolume, RoutedEventArgs

### Community 51 - ".ExportPlayList_Click"
Cohesion: 0.25
Nodes (3): ExportPlayList, TextChangedEventArgs, MenuFlyoutItem

### Community 52 - "ArtistDetailPage"
Cohesion: 0.10
Nodes (10): Button, FrameworkElement, NavigatingCancelEventArgs, NavigationEventArgs, ObservableCollection, Page, SizeChangedEventArgs, Song (+2 more)

### Community 54 - "YearDetailPage"
Cohesion: 0.10
Nodes (10): Button, FrameworkElement, NavigatingCancelEventArgs, NavigationEventArgs, ObservableCollection, Page, SizeChangedEventArgs, Song (+2 more)

### Community 55 - "MostPlayed"
Cohesion: 0.12
Nodes (10): CompactViewStyle, ListViewStyle, Button, List, NavigationEventArgs, ObservableCollection, Page, Song (+2 more)

### Community 57 - ".RunCatchUpDiff"
Cohesion: 0.19
Nodes (11): List, Task, AutoScanReconciler, bool, IEnumerable, List, Song, Task (+3 more)

### Community 58 - "DropDownButton"
Cohesion: 0.10
Nodes (21): SortDropDown, ViewButton, SortDropDown, SortDropDown, ViewButton, SortDropDown, ViewButton, SortDropDown (+13 more)

### Community 59 - "MusicPlayer"
Cohesion: 0.13
Nodes (13): BackendType, bool, IMediaBackend, int, MediaPlayer, Player, RepeatMode, ShuffleMode (+5 more)

### Community 60 - "Button"
Cohesion: 0.14
Nodes (8): AcceptSyncButton, CloseLyricsButton, LyricsMenuButton, MusicInfoButton, SaveAsOffsetButton, SaveByTimestampsButton, Task, Button

### Community 61 - "ImportExportPlaylist"
Cohesion: 0.22
Nodes (6): List, name, Task, ImportExportPlaylist, totalTrackCount, trackInLibrary

### Community 62 - "ContentDialog"
Cohesion: 0.10
Nodes (20): DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog (+12 more)

### Community 63 - "TileListPageBase"
Cohesion: 0.10
Nodes (21): AlphabetNavigationPanel, ArtistTextBlock, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText, Descending, GoToSettings (+13 more)

### Community 64 - "TileListPageBase"
Cohesion: 0.11
Nodes (19): AlphabetNavigationPanel, ContentGrid, CustomProgressBar, DeleteDialogText, GenreTextBlock, GoToSettings, GoToSettingsTextBlock, LoadingProgress (+11 more)

### Community 65 - "ShellNotificationWindow"
Cohesion: 0.15
Nodes (17): bool, Dictionary, DispatcherQueue, int, string, APPBARDATA, MARGINS, MONITORINFOEX (+9 more)

### Community 66 - "AlbumTintProgressOverlay"
Cohesion: 0.12
Nodes (16): AccentColorAnalyzer, BaseColorAnalyzer, Border, Brush, Button, Color, ColorAnalyzer, ColorWeightAnalyzer (+8 more)

### Community 67 - "MainWindow"
Cohesion: 0.14
Nodes (8): AppWindow, WindowEx, SystemTrayIcon, SystemTrayIconEventArgs, void, MainWindow, OverlappedPresenter, WindowEventArgs

### Community 68 - "Type"
Cohesion: 0.18
Nodes (7): Type, DateFormatConverter, DurationConverter, DurationToFullTimeConverter, RelativeTimeConverter, Tunetastic.Common.Converters, IValueConverter

### Community 69 - "TunetasticPageBase"
Cohesion: 0.16
Nodes (10): Page, ToggleButton, DispatcherQueue, Grid, List, Rectangle, StackPanel, Task (+2 more)

### Community 70 - "MenuFlyout"
Cohesion: 0.11
Nodes (18): Sort, More, Sort, More, ViewStyle, More, Sort, MenuFlyout (+10 more)

### Community 71 - "Tunetastic.Views.Common"
Cohesion: 0.21
Nodes (3): Tunetastic.Views.PlaylistViews, Tunetastic.Views.LibraryViews, Tunetastic.Views.Common

### Community 72 - "SettingViewModel"
Cohesion: 0.29
Nodes (6): ObservableObject, bool, RelayCommand, string, Task, SettingViewModel

### Community 73 - "VisualState"
Cohesion: 0.12
Nodes (17): Normal, PointerOver, Pressed, Selected, Normal, PointerOver, Pressed, Selected (+9 more)

### Community 74 - "AccentAncientScrollOverlay"
Cohesion: 0.17
Nodes (11): await, LinearGradientBrush, AccentColorAnalyzer, Border, Color, ColorAnalyzer, Image, SolidColorBrush (+3 more)

### Community 75 - "LrcParser"
Cohesion: 0.21
Nodes (7): TimeSpan, LrcLine, List, Regex, LrcParser, Match, CancelSyncButton

### Community 76 - "WindowsMediaBackend"
Cohesion: 0.15
Nodes (7): bool, double, MediaPlayer, Task, Timer, WindowsMediaBackend, MediaPlaybackSession

### Community 77 - ".Search"
Cohesion: 0.20
Nodes (5): IEnumerable, SearchCategory, SearchItem, SearchResults, SearchScope

### Community 78 - "TileListPageBase"
Cohesion: 0.16
Nodes (7): Button, FrameworkElement, IOrderedEnumerable, ListViewBase, RoutedEventArgs, Task, TileListPageBase

### Community 79 - "MenuFlyoutSubItem"
Cohesion: 0.13
Nodes (15): AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist (+7 more)

### Community 80 - "PlayListTemplate"
Cohesion: 0.14
Nodes (7): Button, List, ListView, ObservableCollection, RoutedEventArgs, Song, PlayListTemplate

### Community 81 - ".GetSuggestions"
Cohesion: 0.19
Nodes (7): AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs, AutoSuggestBoxSuggestionChosenEventArgs, AutoSuggestBoxTextChangedEventArgs, KeyValuePair, SearchItemType, Task

### Community 83 - "DiskSpeedDetector"
Cohesion: 0.32
Nodes (4): TimeSpan, DiskSpeedDetector, DiskKind, Func

### Community 84 - "FlyleafMediaBackend"
Cohesion: 0.14
Nodes (7): bool, long, Player, Task, FlyleafMediaBackend, OpenCompletedArgs, PropertyChangedEventArgs

### Community 86 - "Build"
Cohesion: 0.29
Nodes (5): Build(), BitmapImage, Grid, UpdateTrack(), UIElement

### Community 87 - ".EditInfoSaveButtonEnableUpdate"
Cohesion: 0.24
Nodes (6): LyricsTextBox, PlaylistNameBox, TitleTextBox, TextChangedEventArgs, TextBox, YearNumberBox

### Community 88 - ".Info"
Cohesion: 0.20
Nodes (4): GlobalNotification, IconSource, MultiSelectButton, RoutedEventArgs

### Community 89 - ".DetectRenamesAndMoves"
Cohesion: 0.18
Nodes (10): Dictionary, FileScanMeta, List, RenameDetector, RenameMatchResult, CreationTimeUtc, FileSizeBytes, LastModifiedUtc (+2 more)

### Community 90 - ".ResumeIfEnabled"
Cohesion: 0.30
Nodes (3): Task, AutoScanService, Task

### Community 91 - "RoutedEventArgs"
Cohesion: 0.20
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 92 - "GenreDetailPage"
Cohesion: 0.13
Nodes (8): MultiSelectButton, Button, ListView, NavigatingCancelEventArgs, ObservableCollection, RoutedEventArgs, Song, GenreDetailPage

### Community 93 - "RoutedEventArgs"
Cohesion: 0.20
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 94 - "Tunetastic.Common.Services"
Cohesion: 0.18
Nodes (4): PlaybackStateChangedArgs, Tunetastic.Common.Services, EventArgs, PlaybackState

### Community 95 - ".Create"
Cohesion: 0.18
Nodes (7): Tunetastic.Overlay, OverlayLayout, OverlayTheme, OverlayFactory, IReadOnlyList, OverlayLayoutCatalog, OverlayLayoutInfo

### Community 97 - "RoutedEventArgs"
Cohesion: 0.22
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 98 - ".UpdateListBasedOnMaxLimit"
Cohesion: 0.22
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 99 - "IMediaBackend"
Cohesion: 0.20
Nodes (4): Task, IMediaBackend, Tunetastic.Common.Services.Backends, IDisposable

### Community 102 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): PlayListSongsCompactView, PlayListSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 103 - "ComboBox"
Cohesion: 0.22
Nodes (9): ActualHeight, All, Backdrop, LRCOffsetStandard, TaskBarOverlayDesign, TaskBarOverlayPosition, TaskBarOverlayTheme, Theme (+1 more)

### Community 105 - ".TileView_SelectionChanged"
Cohesion: 0.14
Nodes (8): SelectionChangedEventArgs, SelectionChangedEventArgs, ArtistTileView, ItemClickEventArgs, SelectionChangedEventArgs, ItemClickEventArgs, SelectionChangedEventArgs, YearTileView

### Community 106 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 107 - "Page"
Cohesion: 0.17
Nodes (9): CustomProgressBar, Page, ProgressFill, ProgressFillText, SplashImage, Task, SplashScreen, Grid (+1 more)

### Community 108 - "OverlayGridCreation"
Cohesion: 0.24
Nodes (11): ProcessKeyboardAcceleratorEventArgs, UISettings, OverlayTheme, UIElement, keyboardInput(), NextSong(), OverlayGridCreation(), PreviousSong() (+3 more)

### Community 109 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 110 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (4): Ascending, Descending, GenreModel, Task

### Community 111 - ".UpdateListBasedOnSorting"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 112 - ".OnLibraryReadyAsync"
Cohesion: 0.28
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 113 - ".BuildFileScanMeta"
Cohesion: 0.46
Nodes (4): Task, FileChangeProcessor, FileScanMeta, FileChangeType

### Community 114 - ".ProcessPendingTagWritesAsync"
Cohesion: 0.36
Nodes (3): List, Song, Task

### Community 115 - "PlaybackTracker"
Cohesion: 0.29
Nodes (3): DateTime, TimeSpan, PlaybackTracker

### Community 116 - "RoutedEventArgs"
Cohesion: 0.15
Nodes (8): ClearLyricsButton, CopyLyricsButton, EditLyricsButton, IncreaseButton, OpenLyricsButton, SyncLyricsButton, RoutedEventArgs, AppBarButton

### Community 117 - "Button"
Cohesion: 0.25
Nodes (5): BuyMeACoffee, FullScanButton, GitHubIssues, RateThisAppButton, Button

### Community 118 - ".ApplyAndSaveTint"
Cohesion: 0.29
Nodes (4): ColorChangedEventArgs, ColorPaletteColorChangedEventArgs, ColorPicker, Color

### Community 119 - "AlbumTintOverlay"
Cohesion: 0.09
Nodes (19): AccentColorAnalyzer, BaseColorAnalyzer, Border, Button, ColorAnalyzer, ColorWeightAnalyzer, Image, Rectangle (+11 more)

### Community 120 - "Slider"
Cohesion: 0.29
Nodes (7): VolumeSlider, Slider, AutoAdvanceSlider, MainPlayerBlurSlider, ManualTrackChangeSlider, PlayPauseStopFadeSlider, RainbowSpeedSlider

### Community 121 - ".UpdateListBasedOnViewStyle"
Cohesion: 0.33
Nodes (3): CompactViewStyle, ListViewStyle, Task

### Community 122 - ".PlayAllButton_OnClick"
Cohesion: 0.29
Nodes (5): MoreButton, PlayAll, SettingsButton, ShuffleAndPlay, Button

### Community 124 - "Build"
Cohesion: 0.47
Nodes (5): Build(), BitmapImage, Grid, UpdateToolTipText(), UpdateTrack()

### Community 125 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): ArtistDetailCompactView, ArtistDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 126 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 127 - "ToggleButton"
Cohesion: 0.29
Nodes (5): MultiSelectButton, MultiSelectButton, ShuffleButton, ToggleButton, MultiSelectButton

### Community 128 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): GenreDetailCompactView, GenreDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 129 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 130 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 131 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): ItemClickEventArgs, SelectionChangedEventArgs, YearDetailCompactView, YearDetailListView

### Community 132 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 133 - "ListView"
Cohesion: 0.24
Nodes (7): AlbumTileView, ItemClickEventArgs, MostPlayedSongsCompactView, MostPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs, ListView

### Community 134 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 135 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyAddedSongsCompactView, RecentlyAddedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 136 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 137 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyPlayedSongsCompactView, RecentlyPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 138 - "SettingsCard"
Cohesion: 0.33
Nodes (6): AutoAdvanceCard, IgnoreTrack, ManualTrackChangeCard, PlayPauseStopFadeCard, RainbowSpeed, SettingsCard

### Community 139 - "VisualStateGroup"
Cohesion: 0.40
Nodes (5): CommonStates, CommonStates, CommonStates, CommonStates, VisualStateGroup

### Community 140 - "MusicControl"
Cohesion: 0.31
Nodes (3): MusicControlViewModel, Storyboard, MusicControl

### Community 141 - "GenreTileView"
Cohesion: 0.40
Nodes (3): GenreTileView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 142 - "RepeatButton"
Cohesion: 0.50
Nodes (4): DecreaseButton, ForwardButton, RewindButton, RepeatButton

### Community 143 - "SongListViewModel"
Cohesion: 0.32
Nodes (4): Tunetastic.ViewModels, ObservableCollection, Song, SongListViewModel

### Community 145 - "PART_TextBox"
Cohesion: 0.50
Nodes (3): PART_TextBox, ResourceDictionary, TextBox

### Community 146 - "AutoScrollView"
Cohesion: 0.50
Nodes (4): AutoScrollHeader, AutoScrollView, AutoScrollHeader, AutoScrollHeader

### Community 148 - "TogglePlayPause"
Cohesion: 0.25
Nodes (8): KeyRoutedEventArgs, SystemTrayIcon, SystemTrayIconEventArgs, Task, LoadLastPlayedTrack(), OnTrayIconLeftClick(), PreviewKeyDownMusicControl(), TogglePlayPause()

### Community 151 - "CoverArtImage"
Cohesion: 0.67
Nodes (3): CoverArtImage, SongCoverImage, Image

### Community 152 - "MusicControlsArea"
Cohesion: 0.67
Nodes (3): MusicControlsArea, NavFrame, Frame

### Community 153 - ".Page_Loaded"
Cohesion: 0.25
Nodes (3): ContainerContentChangingEventArgs, ListViewBase, SizeChangedEventArgs

### Community 164 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): AllSongsCompactView, AllSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 166 - "CurrentDurationConverter"
Cohesion: 0.67
Nodes (3): DurationConverter, Position, CurrentDurationConverter

## Knowledge Gaps
- **138 isolated node(s):** `ArtistRuleType`, `BackendType`, `DiskKind`, `FadeType`, `FileChangeType` (+133 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **26 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Tunetastic.Common.Services` connect `Tunetastic.Common.Services` to `.ResumeIfEnabled`, `GlobalUsings.cs`?**
  _High betweenness centrality (0.140) - this node is a cross-community bridge._
- **Why does `SettingsPage` connect `SettingsPage` to `.Ext_ToggleSwitch_OnToggled`, `TunetasticPageBase`, `Page`, `FullscreenStateService`, `RoutedEventArgs`, `Button`, `.ApplyAndSaveTint`, `.NumberBox_ValueChanged`, `CheckForUpdates`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Why does `Tunetastic.Common.Services.TaskbarOverlay` connect `FullscreenStateService` to `MusicControlViewModel.cs`, `TaskbarOverlayManager`, `TaskbarInfo`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **What connects `ArtistRuleType`, `BackendType`, `DiskKind` to the rest of the system?**
  _138 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MusicControlViewModel.cs` be split into smaller, more focused modules?**
  _Cohesion score 0.12554112554112554 - nodes in this community are weakly interconnected._
- **Should `AudioService` be split into smaller, more focused modules?**
  _Cohesion score 0.05084745762711865 - nodes in this community are weakly interconnected._
- **Should `TextBlock` be split into smaller, more focused modules?**
  _Cohesion score 0.05827067669172932 - nodes in this community are weakly interconnected._