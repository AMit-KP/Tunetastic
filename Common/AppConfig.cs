using Nucs.JsonSettings.Examples;
using Nucs.JsonSettings.Modulation;

namespace Tunetastic.Common;
[GenerateAutoSaveOnChange]
public partial class AppConfig : NotifiyingJsonSettings, IVersionable
{
    [EnforcedVersion("1.0.0.0")]
    public Version Version { get; set; } = new Version(1, 0, 0, 0);

    private string fileName { get; set; } = Constants.AppConfigPath;

    private string lastUpdateCheck { get; set; }

    // Docs: https://github.com/Nucs/JsonSettings
}
