using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;

namespace Tunetastic.Common.Helpers;

[GenerateAutoSaveOnChange]
public partial class AppConfig : NotifiyingJsonSettings, IVersionable
{
	/// <summary>
	/// Gets or sets the version of the application configuration.
	/// </summary>
	[EnforcedVersion("1.0.0.0")]
	public Version Version { get; set; } = new Version(1, 0, 0, 0);

	/// <summary>
	/// Gets or sets the file name for the application configuration.
	/// </summary>
	private string fileName { get; set; } = Constants.AppConfigPath;

	/// <summary>
	/// Gets or sets the date and time of the last update check.
	/// </summary>
	private string lastUpdateCheck { get; set; } = string.Empty;
}
