using Nucs.JsonSettings;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;

namespace Tunetastic.Common;
public static partial class AppHelper
{
	public static AppConfig Settings = JsonSettings.Configure<AppConfig>()
							   .WithRecovery(RecoveryAction.RenameAndLoadDefault)
							   .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
							   .LoadNow();
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
}
