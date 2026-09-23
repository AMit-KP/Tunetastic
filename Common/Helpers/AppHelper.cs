using Nucs.JsonSettings;
using Nucs.JsonSettings.Fluent;
using Nucs.JsonSettings.Modulation;
using Nucs.JsonSettings.Modulation.Recovery;

namespace Tunetastic.Common.Helpers;

public static partial class AppHelper
{
	/// <summary>
	/// Gets the application configuration settings.
	/// </summary>
	/// <remarks>
	/// This property provides access to the application's configuration settings
	/// which are automatically loaded and saved using JSON serialization.
	/// </remarks>
	public static AppConfig Settings = JsonSettings.Configure<AppConfig>()
							   .WithRecovery(RecoveryAction.RenameAndLoadDefault)
							   .WithVersioning(VersioningResultAction.RenameAndLoadDefault)
							   .LoadNow();
}
