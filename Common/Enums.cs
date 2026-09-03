namespace Tunetastic.Common;

// ─────────────────────────────────────────────────────────────
//  Playback enums
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Specifies the repeat modes that can be used by the music player.
/// This enum controls how playback behaves when the end of the playlist is reached.
/// It includes options for disabling repeat, repeating a single track, or repeating the entire playlist.
/// </summary>
public enum RepeatMode { None, One, All }

/// <summary>
/// Specifies the shuffle modes available for the music player.
/// This enum defines whether the playlist should be played in sequential order or in a randomized order.
/// The shuffle mode impacts the playback sequence when enabled.
/// </summary>
public enum ShuffleMode { Off, On }

public enum FadeType { None, Manual, AutoAdvance }

// ─────────────────────────────────────────────────────────────
//  Backend enums
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Unified playback state (replaces FlyleafLib.MediaPlayer.Status)
/// </summary>
public enum PlaybackState { Playing, Paused, Stopped, Ended }

/// <summary>
/// Backend type enum
/// </summary>
public enum BackendType { Windows, Flyleaf, Unsupported }

// ─────────────────────────────────────────────────────────────
//  Database/Search enums
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Represents the categories of items that can be searched in the Tunetastic application.
/// This enumeration is used to specify the type of a search item, allowing differentiation
/// between titles, artists, and albums during search operations and result categorization.
/// </summary>
public enum SearchItemType
{
	Title,
	Artist,
	Album
}

/// <summary>
/// Represents the properties of a song that can be used for sorting or filtering operations.
/// </summary>
public enum SongProperty
{
	Title,
	Artists,
	Album,
	Path,
	Genre,
	Year,
	PlayCount,
	Cover,
	Duration,
	DateAdded,
	DateLastPlayed,
	Extension
}

/// <summary>
/// Represents the types of rules that can be applied to manage artist-related data processing and categorization.
/// This enumeration is used within the application to distinguish between different types of artist handling rules,
/// such as splitting artists or marking exceptions in the processing workflow.
/// </summary>
public enum ArtistRuleType
{
	Splitter,
	Exception
}

public enum SearchScope
{
	All,
	Title,
	Artist,
	Album
}

// ─────────────────────────────────────────────────────────────
//  Infrastructure/Helper enums
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Detected storage media category, ordered from slowest to fastest.
/// </summary>
public enum DiskKind
{
	Unknown,
	HDD,
	SataSSD,
	NvmeSSD,
}

/// <summary>
/// Represents the folder locations where thumbnail images are stored.
/// </summary>
public enum ThumbnailFolder
{
	AllSongView,
	MainPlayer
}

/// <summary>
/// Enum representing the keys for storing and retrieving local settings or preferences within the application.
/// These values are utilized to save user-defined configurations and application data in persistent storage.
/// </summary>
public enum LocalSave
{
	IgnoreDuplicateEnabled,
	ScanAtStartup,
	IgnoreTracksBelowDuration,
	ScanResult,
	PlayPauseStopFadeStatus,
	PlayPauseStopFadeValue,
	AutoAdvanceStatus,
	AutoAdvanceValue,
	ManualTrackChangeStatus,
	ManualTrackChangeValue,
	PreviousResetStatus,
	RestartTrackOnSelectionStatus,
	UseSystemVolumeStatus,
	PauseOnMuteStatus,
	AutoStartStatus,
	MainPlayerBGBlurValue,
	LastPlayedTrack,
	PlayBackPosition,
	CurrentIndex,
	CurrentPlayinglist,
	ShuffleStatus,
	RepeatStatus,
	Backdrop,
	Theme,
	BackdropTintColorStatus,
	BackdropTintColorA,
	BackdropTintColorR,
	BackdropTintColorG,
	BackdropTintColorB,
	RainbowFrameStatus,
	RainbowFrameSpeed,
	RainbowOnlyDuringPlayback,
	AllSongViewSortBy,
	AllSongViewSortOrder,
	AllSongViewStyle,
	MinimizeToTray,
	RecentlyAddedSongViewStyle,
	RecentlyAddedMaxLimit,
	MostPlayedSongViewStyle,
	MostPlayedMaxLimit,
	RecentlyAddedSongTimeStyle,
	duplicateQueueAllowed,
	RecentlyPlayedSongViewStyle,
	RecentlyPlayedMaxLimit,
	RecentlyPlayedSongTimeStyle,
	ArtistsEnabled,
	AlbumsEnabled,
	GenresEnabled,
	YearsEnabled,
	RecentlyAddedEnabled,
	RecentlyPlayedEnabled,
	MostPlayedEnabled,
	YearsViewSortOrder,
	YearDetailViewSortBy,
	YearDetailViewSortOrder,
	YearDetailViewStyle,
	SelectedYear,
	GenresViewSortOrder,
	GenreDetailViewSortBy,
	GenreDetailViewSortOrder,
	GenreDetailViewStyle,
	SelectedGenre,
	AlbumsViewSortOrder,
	AlbumDetailViewSortBy,
	AlbumDetailViewSortOrder,
	AlbumDetailViewStyle,
	SelectedAlbum,
	ArtistsViewSortOrder,
	ArtistDetailViewSortBy,
	ArtistDetailViewSortOrder,
	ArtistDetailViewStyle,
	SelectedArtist,
	CheckForUpdatesAtStatup,
	ShowVersionInfoOnTitleBar,
	ForwardRewindButtonVisibility,
	GivenStoreRating,
	TaskBarOverlayStatus,
	TaskBarOverlaySide,
	TaskBarOverlayTheme,
	TaskBarOverlayDesign,
	LRCOffsetSOfficialtandard
}

/// <summary>
/// The visual theme applied to the overlay grid.
/// </summary>
public enum OverlayTheme
{
	Dark,
	Light
}

/// <summary>
/// All available overlay layout styles.
/// Each value has a display name used in user-facing dropdowns.
/// </summary>
public enum OverlayLayout
{
	CompactPill,
	HoverReveal,
	WaveformEdge,
	MarqueeTicker,
	LeftPill,
	RightDock,
	FullArtBar,
	WaveformOnly,
	AccentAncientScroll,
	IconStrip,
	StackedInfo,
	CenteredPill,
	TopAccentStripe,
	BottomAccentStripe,
	AlbumTint,
	TextOnly,
	TextOnlyReversed,
	ArcRing,
	QueuePreview,
	ArtistBadge,
	TopAlbumAccentStripe,
	AlbumTintProgress
}

public enum FileChangeType
{
	Created,
	Modified,
	Deleted,
	Renamed
}
