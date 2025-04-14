using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucs.JsonSettings;

namespace Tunetastic.Common;

public sealed class LibrarySettingsSaver
{
    private static readonly Lazy<LibrarySettingsSaver> _instance =
        new(() => new LibrarySettingsSaver());

    public static LibrarySettingsSaver Instance => _instance.Value;

    public LibrarySettings LibrarySaveSettings { get; }

    private LibrarySettingsSaver()
    {
        LibrarySaveSettings = JsonSettings.Load<LibrarySettings>();
    }

    public void SaveSettings()
    {
        LibrarySaveSettings.Save();
    }
}

