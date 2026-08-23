# Graph Report - Tunetastic  (2026-08-24)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 2695 nodes · 5249 edges · 187 communities (141 shown, 46 thin omitted)
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 180 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `0b66becb`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- NativeMethods
- TextBlock
- Tunetastic
- MusicControlViewModel.cs
- .MakeNextButton
- Page
- Page
- TaskbarOverlayManager
- TaskbarOverlayWindow
- TaskbarInfo
- MainPlayerPage
- DatabaseHelper
- Tunetastic.Overlay.Layouts
- MainPage
- SettingsPage
- Enums.cs
- .Info
- GenresViewPage
- SmoothProgressBar
- Page
- InlineSuggestBox
- Page
- RoutedEventArgs
- PlayListTemplate
- App
- RoutedEventArgs
- StackPanel
- Page
- Page
- Page
- Page
- Page
- Page
- AlbumTintOverlay
- RoutedEventArgs
- Page
- RoutedEventArgs
- Page
- .ScrollToSong
- MenuFlyout
- Page
- .ScrollToSong
- List
- DropDownButton
- YearDetailPage
- MostPlayed
- MusicPlayer
- ImportExportPlaylist
- ContentDialog
- Page
- Page
- ArtistsViewPage
- GenreDetailPage
- AudioService
- AlbumTintProgressOverlay
- AlbumsViewPage
- MainWindow
- Type
- Button
- ArtistDetailPage
- FullscreenStateService
- VisualState
- RoutedEventArgs
- Rectangle
- RadioMenuFlyoutItem
- SessionEventHandler
- AccentAncientScrollOverlay
- Tunetastic.Common.Helpers
- ColorHelper
- WindowsMediaBackend
- .Search
- MenuFlyoutSubItem
- .GetSuggestions
- DiskSpeedDetector
- FlyleafMediaBackend
- .ScrollToSong
- GlobalUsings.cs
- SmartWrapVirtualizingLayout
- LrcParser
- .OnDeviceStateChanged
- .SyncSongArtistsForSong
- .EditInfoSaveButtonEnableUpdate
- RoutedEventArgs
- RecentlyAdded
- RecentlyPlayed
- PlaybackTracker
- SettingViewModel
- TopAlbumAccentStripeOverlay
- RoutedEventArgs
- Page
- .ReloadArtistSplitRules
- Tunetastic.Views.PlaylistViews
- AllSongsViewPage
- .PlayAllButton_OnClick
- IMediaBackend
- .LoadSong
- Tunetastic.Views.LibraryViews
- ComboBox
- Song
- ShellNotificationWindow
- MusicControl
- OverlayBase
- .AdjustAlphabetSize
- .AdjustAlphabetSize
- .AdjustAlphabetSize
- .ProcessPendingTagWritesAsync
- Tunetastic.Common.Services.TaskbarOverlay
- AlbumDetailPage
- .SortButton_OnClick
- RoutedEventArgs
- .Page_Loaded
- .ScrollToSong
- .SyncLyricsButton_Click
- .ScrollToSong
- .ApplyAndSaveTint
- .ScanLibraries
- .FindAllAppSessions
- .UpdateListBasedOnSorting
- .Page_Loaded
- ListView
- Slider
- .ExportPlayList_Click
- BuyMeACoffee
- .WaitAndSubscribeToAppVolumeAsync
- PlaybackStateChangedArgs
- .ListView_SelectionChanged
- .ShuffleAndPlayButton_OnClick
- .ListView_SelectionChanged
- Page
- .PlayAllButton_OnClick
- .PlayAllButton_OnClick
- ToggleButton
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .PlayAllButton_OnClick
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .PlayAllButton_OnClick
- .ListView_SelectionChanged
- .TimeStyle_Click
- .Album_Tapped
- AlbumTileView
- VisualStateGroup
- .Artist_Tapped
- ArtistTileView
- .Genre_Tapped
- GenreTileView
- .Year_Tapped
- YearTileView
- SettingsExpander
- PART_TextBox
- AutoScrollView
- .UpdateShimmerPlaceholderCount
- .ListView_ItemClick
- BottomAccentStripeOverlay.cs
- FullArtBarOverlay.cs
- AppData.json
- .NumberBox_ValueChanged
- .SortButton_OnClick
- SongLastPlayed
- .ViewButton_OnClick
- .TimeStyle_Click
- Converters.xaml
- Fonts.xaml
- ThemeResources.xaml
- .Page_ActualThemeChanged
- .OnNavigatingFrom
- .Page_ActualThemeChanged
- .OnNavigatingFrom
- .OnNavigatedTo
- .OnNavigatingFrom
- .Page_ActualThemeChanged
- .OnNavigatingFrom
- AppIcon
- AppTitle
- NavView
- IgnoretracksDuration
- .MinimizeToTray_OnToggled
- .TaskBarOverlay_Toggled
- QueuePreviewOverlay.cs
- TopAccentStripeOverlay.cs
- .ViewButton_OnClick
- .OnNavigatedTo

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
- `App` --inherits--> `Application`  [EXTRACTED]
  App.xaml.cs → App.xaml
- `TaskbarOverlayWindow` --inherits--> `WindowEx`  [EXTRACTED]
  Common/Services/TaskbarOverlay/TaskbarOverlayWindow.cs → MainWindow.xaml
- `MainWindow` --inherits--> `WindowEx`  [EXTRACTED]
  MainWindow.xaml.cs → MainWindow.xaml
- `ActualAlbumGroup` --references--> `TextBlock`  [EXTRACTED]
  Views/LibraryViews/AlbumDetailPage.xaml → Views/MainPage.xaml
- `DeleteDialogText` --references--> `TextBlock`  [EXTRACTED]
  Views/LibraryViews/AlbumDetailPage.xaml → Views/MainPage.xaml

## Import Cycles
- None detected.

## Communities (187 total, 46 thin omitted)

### Community 0 - "NativeMethods"
Cohesion: 0.08
Nodes (28): APPBARDATA, int, IntPtr, string, SUBCLASSPROC, ABE, ABM, ABS (+20 more)

### Community 1 - "TextBlock"
Cohesion: 0.06
Nodes (57): Key, Value, AddPlaylistDialogDescription, AlbumChanged, AlbumGrid, AlbumTeachingTip, AlbumTeachingTipContent, AppTitleBar (+49 more)

### Community 2 - "Tunetastic"
Cohesion: 0.04
Nodes (55): net9.0-windows10.0.26100.0, CommunityToolkit.Common (8.4.2), CommunityToolkit.HighPerformance (8.4.2), CommunityToolkit.Labs.WinUI.Controls.DataTable (0.1.251217-build.2433), CommunityToolkit.Labs.WinUI.Shimmer (0.1.250811-build.2202), CommunityToolkit.Mvvm (8.4.2), CommunityToolkit.WinUI.Animations (8.2.251219), CommunityToolkit.WinUI.Behaviors (8.2.251219) (+47 more)

### Community 3 - "MusicControlViewModel.cs"
Cohesion: 0.06
Nodes (41): Tunetastic.Overlay, OverlayLayout, OverlayTheme, OverlayFactory, IReadOnlyList, OverlayLayoutCatalog, OverlayLayoutInfo, PlaybackStateChangedArgs (+33 more)

### Community 4 - ".MakeNextButton"
Cohesion: 0.13
Nodes (32): Grid, Grid, Build(), Grid, Build(), Grid, Build(), Grid (+24 more)

### Community 5 - "Page"
Cohesion: 0.07
Nodes (38): FontSize, AboutTextBlock, AlbumsToggle, AppearanceTextBlock, ArtistsToggle, AudioTextBlock, AutoAdvanceCard, AutoAdvanceSwitch (+30 more)

### Community 6 - "Page"
Cohesion: 0.06
Nodes (41): Album, Artist, BackgroundImage, BlurBorder, BlurEffect, CoverArt, CoverArtImage, CoverArtProjection (+33 more)

### Community 7 - "TaskbarOverlayManager"
Cohesion: 0.08
Nodes (16): bool, Brush, Dictionary, DispatcherQueue, DispatcherTimer, int, IntPtr, IReadOnlyList (+8 more)

### Community 8 - "TaskbarOverlayWindow"
Cohesion: 0.09
Nodes (14): OverlayRect, bool, DispatcherTimer, Grid, int, IntPtr, PointerRoutedEventArgs, SUBCLASSPROC (+6 more)

### Community 9 - "TaskbarInfo"
Cohesion: 0.11
Nodes (18): ABE, Dictionary, HashSet, int, IntPtr, IReadOnlyList, List, FreeZone (+10 more)

### Community 10 - "MainPlayerPage"
Cohesion: 0.09
Nodes (16): Compositor, MusicPlayer, BitmapImage, bool, Button, DispatcherQueue, DispatcherTimer, double (+8 more)

### Community 11 - "DatabaseHelper"
Cohesion: 0.10
Nodes (9): HashSet, LibraryModel, Regex, Task, AlbumNameRow, ArtistNameRow, DatabaseHelper, SearchCategory (+1 more)

### Community 12 - "Tunetastic.Overlay.Layouts"
Cohesion: 0.06
Nodes (21): Tunetastic.Overlay.Layouts, BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage, UpdateTrack(), BitmapImage (+13 more)

### Community 13 - "MainPage"
Cohesion: 0.08
Nodes (12): NavigationView, NavigationViewItemInvokedEventArgs, NavigationViewSelectionChangedEventArgs, RectInt32, ArtistSplitRule, bool, FrameworkElement, List (+4 more)

### Community 14 - "SettingsPage"
Cohesion: 0.09
Nodes (11): SettingViewModel, DependencyPropertyChangedEventArgs, FrameworkElement, LibraryModel, MusicFormatModel, ObservableCollection, OverlayLayout, RangeBaseValueChangedEventArgs (+3 more)

### Community 15 - "Enums.cs"
Cohesion: 0.07
Nodes (33): string, Constants, ArtistRuleType, BackendType, DiskKind, FadeType, LocalSave, OverlayLayout (+25 more)

### Community 16 - ".Info"
Cohesion: 0.10
Nodes (13): bool, ContainerContentChangingEventArgs, DispatcherQueue, List, ListViewBase, NavigatingCancelEventArgs, NavigationEventArgs, ObservableCollection (+5 more)

### Community 17 - "GenresViewPage"
Cohesion: 0.08
Nodes (17): Ascending, Descending, bool, ContainerContentChangingEventArgs, DispatcherQueue, FrameworkElement, GenreModel, IOrderedEnumerable (+9 more)

### Community 18 - "SmoothProgressBar"
Cohesion: 0.10
Nodes (15): bool, Brush, DependencyProperty, DependencyPropertyChangedEventArgs, DispatcherTimer, double, Grid, PointerRoutedEventArgs (+7 more)

### Community 19 - "Page"
Cohesion: 0.11
Nodes (19): AlphabetNavigationPanel, ContentGrid, CustomProgressBar, DeleteDialogText, GenreTextBlock, GoToSettings, GoToSettingsTextBlock, LoadingProgress (+11 more)

### Community 20 - "InlineSuggestBox"
Cohesion: 0.11
Nodes (17): ArtistSplitRule, bool, DependencyProperty, int, KeyRoutedEventArgs, List, PointerRoutedEventArgs, RoutedEventArgs (+9 more)

### Community 21 - "Page"
Cohesion: 0.09
Nodes (25): CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200, Limit50 (+17 more)

### Community 22 - "RoutedEventArgs"
Cohesion: 0.09
Nodes (14): AlbumTextBox, ArtistTextBox, BrowseCoverArtButton, ClearButton, GenreAutoSuggestBox, OpenContainingFolderButton, RemoveCoverArtButton, SearchBox (+6 more)

### Community 23 - "PlayListTemplate"
Cohesion: 0.13
Nodes (7): MultiSelectButton, DispatcherQueue, List, ListView, ObservableCollection, RoutedEventArgs, PlayListTemplate

### Community 24 - "App"
Cohesion: 0.08
Nodes (17): Application, IntPtr, SystemTrayIcon, App, AudioService, Tunetastic, IJsonNavigationService, IRainbowFrame (+9 more)

### Community 25 - "RoutedEventArgs"
Cohesion: 0.15
Nodes (4): MultiSelectButton, IOrderedEnumerable, ListView, RoutedEventArgs

### Community 26 - "StackPanel"
Cohesion: 0.11
Nodes (25): ActualYearGroup, AlbumSort, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+17 more)

### Community 27 - "Page"
Cohesion: 0.09
Nodes (26): CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200 (+18 more)

### Community 28 - "Page"
Cohesion: 0.10
Nodes (24): ActualGenreGroup, AlbumSort, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar, DeleteDialogText (+16 more)

### Community 29 - "Page"
Cohesion: 0.10
Nodes (23): ActualAlbumGroup, AlbumDetailCompactViewGrid, AlbumDetailListViewGrid, AlphabetNavigationPanel, ArtistsSort, Ascending, ContentGrid, CustomProgressBar (+15 more)

### Community 30 - "Page"
Cohesion: 0.10
Nodes (23): ActualArtistGroup, AlbumSort, AlphabetNavigationPanel, ArtistDetailCompactViewGrid, ArtistDetailListViewGrid, Ascending, ContentGrid, CustomProgressBar (+15 more)

### Community 31 - "Page"
Cohesion: 0.09
Nodes (24): CustomProgressBar, DeleteDialogText, ErrorMessage, ExportErrorMessage, ExportFormat, ExportTextBox, GoToSettings, GoToSettingsTextBlock (+16 more)

### Community 32 - "Page"
Cohesion: 0.09
Nodes (29): CustomProgressBar, DateTooltip, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, Header, Limit100, Limit200 (+21 more)

### Community 33 - "AlbumTintOverlay"
Cohesion: 0.18
Nodes (11): AccentColorAnalyzer, BaseColorAnalyzer, Border, Button, ColorAnalyzer, ColorWeightAnalyzer, Image, Rectangle (+3 more)

### Community 34 - "RoutedEventArgs"
Cohesion: 0.08
Nodes (8): CheckForUpdates, CheckForUpdatesStartupToggle, ForwardRewindButtonVisibility, ScanAtStart, UseSystemVolume, VersionInfoToggle, RoutedEventArgs, ProgressButton

### Community 35 - "Page"
Cohesion: 0.10
Nodes (22): DurationConverter, Position, AlbumCover, BottomProgressBar, CurrentDurationConverter, ForwardButton, MusicControls, NextButton (+14 more)

### Community 36 - "RoutedEventArgs"
Cohesion: 0.19
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 37 - "Page"
Cohesion: 0.09
Nodes (22): AlbumCover, AlbumTextBlock, AlphabetNavigationPanel, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock (+14 more)

### Community 39 - "MenuFlyout"
Cohesion: 0.10
Nodes (21): More, Sort, More, Sort, ViewStyle, More, ViewStyle, Sort (+13 more)

### Community 40 - "Page"
Cohesion: 0.10
Nodes (20): Ascending, ContentGrid, CustomProgressBar, DeleteDialogText, Descending, GoToSettings, GoToSettingsTextBlock, LoadingProgress (+12 more)

### Community 41 - ".ScrollToSong"
Cohesion: 0.23
Nodes (4): CompactViewStyle, ListViewStyle, Song, Task

### Community 42 - "List"
Cohesion: 0.10
Nodes (7): AlbumModel, ArtistModel, ArtistSplitRule, GenreModel, List, MusicFormatModel, YearModel

### Community 43 - "DropDownButton"
Cohesion: 0.10
Nodes (21): SortDropDown, ViewButton, SortDropDown, SortDropDown, ViewButton, SortDropDown, ViewButton, SortDropDown (+13 more)

### Community 44 - "YearDetailPage"
Cohesion: 0.17
Nodes (6): DispatcherQueue, ListView, ObservableCollection, RoutedEventArgs, SelectionChangedEventArgs, YearDetailPage

### Community 45 - "MostPlayed"
Cohesion: 0.17
Nodes (7): DispatcherQueue, List, NavigationEventArgs, ObservableCollection, Song, Task, MostPlayed

### Community 46 - "MusicPlayer"
Cohesion: 0.13
Nodes (13): BackendType, bool, IMediaBackend, int, MediaPlayer, Player, RepeatMode, ShuffleMode (+5 more)

### Community 47 - "ImportExportPlaylist"
Cohesion: 0.22
Nodes (6): List, name, Task, ImportExportPlaylist, totalTrackCount, trackInLibrary

### Community 48 - "ContentDialog"
Cohesion: 0.10
Nodes (20): DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog, DeleteDialog (+12 more)

### Community 49 - "Page"
Cohesion: 0.13
Nodes (17): AllSongsCompactViewGrid, AllSongsListViewGrid, AlphabetNavigationPanel, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock (+9 more)

### Community 50 - "Page"
Cohesion: 0.11
Nodes (19): AlphabetNavigationPanel, ArtistTextBlock, ContentGrid, CustomProgressBar, DeleteDialogText, GoToSettings, GoToSettingsTextBlock, LoadingProgress (+11 more)

### Community 51 - "ArtistsViewPage"
Cohesion: 0.17
Nodes (8): ArtistModel, bool, DispatcherQueue, IOrderedEnumerable, List, ObservableCollection, Task, ArtistsViewPage

### Community 52 - "GenreDetailPage"
Cohesion: 0.17
Nodes (6): DispatcherQueue, FrameworkElement, ListView, ObservableCollection, RoutedEventArgs, GenreDetailPage

### Community 53 - "AudioService"
Cohesion: 0.11
Nodes (7): AudioVolumeNotificationData, bool, AudioService, IMMNotificationClient, MMDeviceEnumerator, object, PropertyKey

### Community 54 - "AlbumTintProgressOverlay"
Cohesion: 0.12
Nodes (16): AccentColorAnalyzer, BaseColorAnalyzer, Border, Brush, Button, Color, ColorAnalyzer, ColorWeightAnalyzer (+8 more)

### Community 55 - "AlbumsViewPage"
Cohesion: 0.16
Nodes (9): MultiSelectButton, bool, DispatcherQueue, double, int, List, ObservableCollection, RoutedEventArgs (+1 more)

### Community 56 - "MainWindow"
Cohesion: 0.14
Nodes (8): AppWindow, WindowEx, SystemTrayIcon, SystemTrayIconEventArgs, void, MainWindow, OverlappedPresenter, WindowEventArgs

### Community 57 - "Type"
Cohesion: 0.18
Nodes (7): Type, DateFormatConverter, DurationConverter, DurationToFullTimeConverter, RelativeTimeConverter, Tunetastic.Common.Converters, IValueConverter

### Community 58 - "Button"
Cohesion: 0.12
Nodes (10): GlobalNotification, AcceptSyncButton, CancelSyncButton, CloseLyricsButton, LyricsMenuButton, MusicInfoButton, SaveAsOffsetButton, SaveByTimestampsButton (+2 more)

### Community 59 - "ArtistDetailPage"
Cohesion: 0.19
Nodes (7): DispatcherQueue, FrameworkElement, NavigatingCancelEventArgs, ObservableCollection, Song, Task, ArtistDetailPage

### Community 60 - "FullscreenStateService"
Cohesion: 0.19
Nodes (5): Dictionary, DispatcherTimer, IReadOnlyList, FullscreenStateService, TaskbarStateService

### Community 61 - "VisualState"
Cohesion: 0.12
Nodes (17): Normal, PointerOver, Pressed, Selected, Normal, PointerOver, Pressed, Selected (+9 more)

### Community 62 - "RoutedEventArgs"
Cohesion: 0.15
Nodes (8): ClearLyricsButton, CopyLyricsButton, DecreaseButton, EditLyricsButton, IncreaseButton, OpenLyricsButton, RoutedEventArgs, AppBarButton

### Community 63 - "Rectangle"
Cohesion: 0.12
Nodes (16): ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill, ProgressFill (+8 more)

### Community 64 - "RadioMenuFlyoutItem"
Cohesion: 0.18
Nodes (12): AlbumSort, ArtistsSort, Ascending, CompactViewStyle, Descending, DurationSort, ListViewStyle, TitleSort (+4 more)

### Community 65 - "SessionEventHandler"
Cohesion: 0.14
Nodes (7): AudioSessionDisconnectReason, AudioSessionState, IntPtr, SessionEventHandler, Guid, IAudioSessionEventsHandler, SessionEventHandler

### Community 66 - "AccentAncientScrollOverlay"
Cohesion: 0.17
Nodes (11): await, LinearGradientBrush, AccentColorAnalyzer, Border, Color, ColorAnalyzer, Image, SolidColorBrush (+3 more)

### Community 67 - "Tunetastic.Common.Helpers"
Cohesion: 0.14
Nodes (9): AppConfig, AppHelper, ImageResizer, Tunetastic.Common.Helpers, IPicture, IVersionable, NotifiyingJsonSettings, ThumbnailFolder (+1 more)

### Community 68 - "ColorHelper"
Cohesion: 0.26
Nodes (6): Color, double, OverlayTheme, ColorHelper, Lab, Lab

### Community 69 - "WindowsMediaBackend"
Cohesion: 0.15
Nodes (7): bool, double, MediaPlayer, Task, WindowsMediaBackend, MediaPlaybackSession, Timer

### Community 70 - ".Search"
Cohesion: 0.20
Nodes (5): IEnumerable, SearchCategory, SearchItem, SearchResults, SearchScope

### Community 71 - "MenuFlyoutSubItem"
Cohesion: 0.13
Nodes (15): AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist, AddToPlaylist (+7 more)

### Community 72 - ".GetSuggestions"
Cohesion: 0.19
Nodes (7): AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs, AutoSuggestBoxSuggestionChosenEventArgs, AutoSuggestBoxTextChangedEventArgs, KeyValuePair, SearchItemType, Task

### Community 73 - "DiskSpeedDetector"
Cohesion: 0.32
Nodes (4): TimeSpan, DiskSpeedDetector, DiskKind, Func

### Community 74 - "FlyleafMediaBackend"
Cohesion: 0.14
Nodes (7): bool, long, Player, Task, FlyleafMediaBackend, OpenCompletedArgs, PropertyChangedEventArgs

### Community 75 - ".ScrollToSong"
Cohesion: 0.21
Nodes (4): IOrderedEnumerable, SizeChangedEventArgs, Song, Task

### Community 76 - "GlobalUsings.cs"
Cohesion: 0.15
Nodes (4): Tunetastic.Common.Operations, Tunetastic.ViewModels, Tunetastic.Common.Infrastructure, Tunetastic.Common.Controls

### Community 77 - "SmartWrapVirtualizingLayout"
Cohesion: 0.24
Nodes (8): double, FrameworkElement, int, Size, LayoutState, SmartWrapVirtualizingLayout, VirtualizingLayout, VirtualizingLayoutContext

### Community 78 - "LrcParser"
Cohesion: 0.26
Nodes (6): TimeSpan, LrcLine, List, Regex, LrcParser, Match

### Community 79 - ".OnDeviceStateChanged"
Cohesion: 0.23
Nodes (4): DataFlow, DeviceState, MMDevice, Role

### Community 81 - ".EditInfoSaveButtonEnableUpdate"
Cohesion: 0.24
Nodes (6): LyricsTextBox, PlaylistNameBox, TitleTextBox, TextChangedEventArgs, TextBox, YearNumberBox

### Community 82 - "RoutedEventArgs"
Cohesion: 0.19
Nodes (3): MultiSelectButton, ListView, RoutedEventArgs

### Community 83 - "RecentlyAdded"
Cohesion: 0.14
Nodes (10): MultiSelectButton, RecentlyAddedPage, DispatcherQueue, DispatcherTimer, List, ListView, ObservableCollection, RoutedEventArgs (+2 more)

### Community 84 - "RecentlyPlayed"
Cohesion: 0.12
Nodes (11): MultiSelectButton, RecentlyPlayedPage, DispatcherQueue, DispatcherTimer, List, ListView, NavigationEventArgs, ObservableCollection (+3 more)

### Community 85 - "PlaybackTracker"
Cohesion: 0.18
Nodes (4): DateTime, TimeSpan, PlaybackTracker, Tunetastic.Common.Services

### Community 86 - "SettingViewModel"
Cohesion: 0.29
Nodes (6): ObservableObject, bool, RelayCommand, string, Task, SettingViewModel

### Community 87 - "TopAlbumAccentStripeOverlay"
Cohesion: 0.17
Nodes (11): AccentColorAnalyzer, BaseColorAnalyzer, Border, ColorAnalyzer, ColorWeightAnalyzer, double, Image, Rectangle (+3 more)

### Community 89 - "Page"
Cohesion: 0.25
Nodes (7): CustomProgressBar, Page, ProgressFill, ProgressFillText, SplashImage, Grid, Image

### Community 92 - "AllSongsViewPage"
Cohesion: 0.16
Nodes (9): DispatcherQueue, FrameworkElement, List, NavigationEventArgs, ObservableCollection, Song, string, Task (+1 more)

### Community 93 - ".PlayAllButton_OnClick"
Cohesion: 0.29
Nodes (5): MoreButton, PlayAll, SettingsButton, ShuffleAndPlay, Button

### Community 94 - "IMediaBackend"
Cohesion: 0.20
Nodes (4): Task, IMediaBackend, Tunetastic.Common.Services.Backends, IDisposable

### Community 97 - "ComboBox"
Cohesion: 0.22
Nodes (9): ActualHeight, All, Backdrop, LRCOffsetStandard, TaskBarOverlayDesign, TaskBarOverlayPosition, TaskBarOverlayTheme, Theme (+1 more)

### Community 99 - "ShellNotificationWindow"
Cohesion: 0.28
Nodes (5): bool, Dictionary, DispatcherQueue, ShellNotificationWindow, WndProcDelegate

### Community 100 - "MusicControl"
Cohesion: 0.31
Nodes (3): MusicControlViewModel, Storyboard, MusicControl

### Community 101 - "OverlayBase"
Cohesion: 0.25
Nodes (5): bool, Grid, OverlayTheme, UIElement, OverlayBase

### Community 102 - ".AdjustAlphabetSize"
Cohesion: 0.22
Nodes (4): CompactViewStyle, ListViewStyle, IOrderedEnumerable, SizeChangedEventArgs

### Community 103 - ".AdjustAlphabetSize"
Cohesion: 0.22
Nodes (4): CompactViewStyle, ListViewStyle, IOrderedEnumerable, SizeChangedEventArgs

### Community 104 - ".AdjustAlphabetSize"
Cohesion: 0.22
Nodes (4): CompactViewStyle, ListViewStyle, IOrderedEnumerable, SizeChangedEventArgs

### Community 105 - ".ProcessPendingTagWritesAsync"
Cohesion: 0.36
Nodes (3): List, Song, Task

### Community 107 - "AlbumDetailPage"
Cohesion: 0.18
Nodes (7): DispatcherQueue, FrameworkElement, NavigatingCancelEventArgs, ObservableCollection, Song, Task, AlbumDetailPage

### Community 108 - ".SortButton_OnClick"
Cohesion: 0.25
Nodes (3): Ascending, Descending, IOrderedEnumerable

### Community 110 - ".Page_Loaded"
Cohesion: 0.25
Nodes (3): ContainerContentChangingEventArgs, ListViewBase, SizeChangedEventArgs

### Community 113 - ".ScrollToSong"
Cohesion: 0.24
Nodes (4): CompactViewStyle, ListViewStyle, Song, Task

### Community 114 - ".ApplyAndSaveTint"
Cohesion: 0.29
Nodes (4): ColorChangedEventArgs, ColorPaletteColorChangedEventArgs, ColorPicker, Color

### Community 115 - ".ScanLibraries"
Cohesion: 0.48
Nodes (4): bool, List, Task, GetMusicData

### Community 116 - ".FindAllAppSessions"
Cohesion: 0.33
Nodes (4): List, Name, Id, Pid

### Community 118 - ".Page_Loaded"
Cohesion: 0.29
Nodes (3): ContainerContentChangingEventArgs, ListViewBase, SizeChangedEventArgs

### Community 119 - "ListView"
Cohesion: 0.38
Nodes (5): ArtistDetailCompactView, ArtistDetailListView, ItemClickEventArgs, SelectionChangedEventArgs, ListView

### Community 120 - "Slider"
Cohesion: 0.29
Nodes (7): VolumeSlider, Slider, AutoAdvanceSlider, MainPlayerBlurSlider, ManualTrackChangeSlider, PlayPauseStopFadeSlider, RainbowSpeedSlider

### Community 121 - ".ExportPlayList_Click"
Cohesion: 0.33
Nodes (3): ExportPlayList, TextChangedEventArgs, MenuFlyoutItem

### Community 122 - "BuyMeACoffee"
Cohesion: 0.29
Nodes (4): BuyMeACoffee, GitHubIssues, RateThisAppButton, Button

### Community 123 - ".WaitAndSubscribeToAppVolumeAsync"
Cohesion: 0.33
Nodes (3): AudioSessionControl, Task, IAudioSessionControl

### Community 124 - "PlaybackStateChangedArgs"
Cohesion: 0.33
Nodes (3): PlaybackStateChangedArgs, EventArgs, PlaybackState

### Community 125 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): AlbumDetailCompactView, AlbumDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 126 - ".ShuffleAndPlayButton_OnClick"
Cohesion: 0.40
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 127 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): AllSongsCompactView, AllSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 128 - "Page"
Cohesion: 0.22
Nodes (5): Page, Page, QueuedList, Task, SplashScreen

### Community 129 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 130 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 131 - "ToggleButton"
Cohesion: 0.25
Nodes (7): MultiSelectButton, MultiSelectButton, MultiSelectButton, MultiSelectButton, MultiSelectButton, ShuffleButton, ToggleButton

### Community 132 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): GenreDetailCompactView, GenreDetailListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 133 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 134 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 135 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 136 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): MostPlayedSongsCompactView, MostPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 137 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): PlayListSongsCompactView, PlayListSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 138 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 139 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyAddedSongsCompactView, RecentlyAddedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 140 - ".PlayAllButton_OnClick"
Cohesion: 0.33
Nodes (4): MoreButton, PlayAll, ShuffleAndPlay, Button

### Community 141 - ".ListView_SelectionChanged"
Cohesion: 0.40
Nodes (4): RecentlyPlayedSongsCompactView, RecentlyPlayedSongsListView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 144 - "AlbumTileView"
Cohesion: 0.40
Nodes (3): AlbumTileView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 145 - "VisualStateGroup"
Cohesion: 0.40
Nodes (5): CommonStates, CommonStates, CommonStates, CommonStates, VisualStateGroup

### Community 147 - "ArtistTileView"
Cohesion: 0.40
Nodes (3): ArtistTileView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 149 - "GenreTileView"
Cohesion: 0.40
Nodes (3): GenreTileView, ItemClickEventArgs, SelectionChangedEventArgs

### Community 151 - "YearTileView"
Cohesion: 0.40
Nodes (3): ItemClickEventArgs, SelectionChangedEventArgs, YearTileView

### Community 152 - "SettingsExpander"
Cohesion: 0.40
Nodes (5): FileExt, LibaryPlayListVisiblity, Scan, TintSettings, SettingsExpander

### Community 153 - "PART_TextBox"
Cohesion: 0.50
Nodes (3): PART_TextBox, ResourceDictionary, TextBox

### Community 154 - "AutoScrollView"
Cohesion: 0.50
Nodes (4): AutoScrollHeader, AutoScrollView, AutoScrollHeader, AutoScrollHeader

### Community 156 - ".ListView_ItemClick"
Cohesion: 0.50
Nodes (3): ItemClickEventArgs, YearDetailCompactView, YearDetailListView

### Community 162 - "SongLastPlayed"
Cohesion: 0.67
Nodes (3): SongLastPlayed, SongPlayCount, Run

### Community 183 - "QueuePreviewOverlay.cs"
Cohesion: 0.67
Nodes (3): BitmapImage, UpdateToolTipText(), UpdateTrack()

## Knowledge Gaps
- **137 isolated node(s):** `ABE`, `ABM`, `ABS`, `QUNS`, `AlbumNameRow` (+132 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **46 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `SettingsPage` connect `SettingsPage` to `Page`, `.NumberBox_ValueChanged`, `RoutedEventArgs`, `Page`, `TaskbarOverlayManager`, `Tunetastic.Common.Services.TaskbarOverlay`, `.ApplyAndSaveTint`, `.MinimizeToTray_OnToggled`, `.TaskBarOverlay_Toggled`, `BuyMeACoffee`?**
  _High betweenness centrality (0.246) - this node is a cross-community bridge._
- **Why does `Tunetastic.Common.Services.TaskbarOverlay` connect `Tunetastic.Common.Services.TaskbarOverlay` to `MusicControlViewModel.cs`, `TaskbarInfo`, `ShellNotificationWindow`, `FullscreenStateService`?**
  _High betweenness centrality (0.167) - this node is a cross-community bridge._
- **Why does `Tunetastic.Views` connect `Tunetastic.Common.Services.TaskbarOverlay` to `Page`, `Tunetastic.Views.PlaylistViews`, `GlobalUsings.cs`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **What connects `ABE`, `ABM`, `ABS` to the rest of the system?**
  _137 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `NativeMethods` be split into smaller, more focused modules?**
  _Cohesion score 0.07773664727657324 - nodes in this community are weakly interconnected._
- **Should `TextBlock` be split into smaller, more focused modules?**
  _Cohesion score 0.05747126436781609 - nodes in this community are weakly interconnected._
- **Should `Tunetastic` be split into smaller, more focused modules?**
  _Cohesion score 0.03636363636363636 - nodes in this community are weakly interconnected._