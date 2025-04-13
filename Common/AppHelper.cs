using Nucs.JsonSettings;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;
using Tunetastic.Models;

namespace Tunetastic.Common;
public static partial class AppHelper
{
    public static AppConfig Settings = JsonSettings.Configure<AppConfig>()
                               .WithRecovery(RecoveryAction.RenameAndLoadDefault)
                               .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
                               .LoadNow();
}

public class LibrarySettings : JsonSettings
{
    public override string FileName { get; set; } = Constants.LibrariesDataPath;

    public virtual List<MusicLibraryPath> LibraryPaths { get; set; } = new();

    public virtual bool IgnoreEnabled { get; set; }

    public virtual bool ScanAtStartup { get; set; }

    public virtual double ignoreTracksBelowDuration { get; set; }

    public virtual string ScanResult { get; set; } = string.Empty;
}


