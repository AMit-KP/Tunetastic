using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls.Primitives;
using Tunetastic.Generated.Protos;
using Tunetastic.Services;

namespace Tunetastic.Views;

public sealed partial class SettingsPage : Page
{
    public ObservableCollection<Library> Libraries
    {
        get; set;
    } = new();

    public ObservableCollection<Format> AllFormats
    {
        get; set;
    } = new();

    public SettingViewModel ViewModel { get; }
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingViewModel>();
        this.InitializeComponent();

        numberBox.ValueChanged += NumberBox_ValueChanged;
        MainPlayerBlurSlider.ValueChanged += MainPlayerBlurSlider_OnValueChanged;

        try
        {
            Libraries.AddRange(ProtobufData.LoadFromBin<LibraryList>(DataFile.AllLibraries).Libraries);
        }
        catch (Exception)
        {
            // ignored
        }

        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        IgnoreDup.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");

        ScanAtStart.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.ScanAtStartup)]?.ToString() ?? "false");

        IgnoreTrack.Description = $"Tracks are ignored if they are less than {localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0"} seconds";

        numberBox.Value = double.Parse(localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");

        Scan.Description = localSettings.Values[nameof(LocalSave.ScanResult)];

        PlayPauseStopFadeSwitch.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.PlayPauseStopFadeStatus)]?.ToString() ?? "false");
        PlayPauseStopFadeSwitch_OnToggled(PlayPauseStopFadeSwitch, null);

        AutoAdvanceSwitch.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.AutoAdvanceStatus)]?.ToString() ?? "false");
        AutoAdvanceSwitch_OnToggled(AutoAdvanceSwitch, null);

        ManualTrackChangeSwitch.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.ManualTrackChangeStatus)]?.ToString() ?? "false");
        ManualTrackChangeSwitch_OnToggled(ManualTrackChangeSwitch, null);

        PreviousReset.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.PreviousResetStatus)]?.ToString() ?? "false");

        RestartTrackOnSelection.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.RestartTrackOnSelectionStatus)]?.ToString() ?? "true");

        UseSystemVolume.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.UseSystemVolumeStatus)]?.ToString() ?? "false");

        PauseOnMute.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.PauseOnMuteStatus)]?.ToString() ?? "true");

        AutoStart.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.AutoStartStatus)]?.ToString() ?? "false");

        MainPlayerBlurSlider.Value = int.Parse(localSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)]?.ToString() ?? 5.ToString());

        UpdateExtentionListOnUI();
    }


    #region Check Later
    //private void OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    //{
    //    TintBox.Fill = new SolidColorBrush(args.NewColor);
    //    App.Current.ThemeService.SetBackdropTintColor(args.NewColor);
    //}

    //private void OnColorPaletteItemClick(object sender, ItemClickEventArgs e)
    //{
    //    var color = e.ClickedItem as ColorPaletteItem;
    //    if (color != null)
    //    {
    //        if (color.Hex.Contains("#000000"))
    //        {
    //            App.Current.ThemeService.ResetBackdropProperties();
    //        }
    //        else
    //        {
    //            App.Current.ThemeService.SetBackdropTintColor(color.Color);
    //        }
    //        TintBox.Fill = new SolidColorBrush(color.Color);
    //    }
    //}
    #endregion

    private async void AddNewFolder_ButtonClick(object sender, RoutedEventArgs e)
    {
        var Hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        var picker = new DevWinUI.FolderPicker(Hwnd);
        picker.Title = "Choose Library Folder(s)";
        picker.CommitButtonText = "Add Folder(s)";
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;

        var musicfolders = await picker.PickMultipleFoldersAsync();

        List<Library> uniqueFolders = new();
        uniqueFolders.AddRange(Libraries);

        foreach (var musicfolder in musicfolders)
        {
            var libraryData = new Library
            {
                Name = musicfolder.Name,
                Path = musicfolder.Path
            };
            uniqueFolders.Add(libraryData);
        }
        uniqueFolders = uniqueFolders.DistinctBy(p => p.Path).ToList();

        Libraries?.Clear();
        Libraries.AddRange(uniqueFolders);
        ProtobufData.SaveToBin<LibraryList>(DataFile.AllLibraries, new LibraryList() { Libraries = { uniqueFolders } });
    }

    private void RemoveFolder_ButtonClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;

        if (button!.CommandParameter is Library library)
            Libraries.Remove(library);

        ProtobufData.SaveToBin<LibraryList>(DataFile.AllLibraries, new LibraryList() { Libraries = { Libraries } });
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ProgressRing.IsActive = true;
        ProgressRing.Visibility = Visibility.Visible;
        Scan.IsEnabled = false;

        await Task.Delay(1000);

        await new GetMusicDataService().UpdateMetaData(true);

        Scan.IsEnabled = true;
        ProgressRing.IsActive = false;
        ProgressRing.Visibility = Visibility.Collapsed;

        Scan.Description = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ScanResult)];
    }

    private void IgnoreDup_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)] = toggleSwitch.IsOn;
    }

    private void ScanAtStart_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ScanAtStartup)] = toggleSwitch.IsOn;
    }

    private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Normal, async () =>
    {
        var numberBox = sender as NumberBox;
        if (numberBox?.Value < 0)
        {
            numberBox.Value = 0;
        }
        IgnoreTrack.Description = $"Tracks are ignored if they are less than {numberBox?.Value} seconds";

        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)] = numberBox?.Value;
    });

    private void UpdateExtentionListOnUI()
    {
        var foramtList = ProtobufData.LoadFromBin<FormatList>(DataFile.FormatsAllowed).Formatlist;
        if (foramtList.Count == 0)
        {
            foramtList.Add(new Format() { Extension = ".mp3", Enabled = true, Description = "MPEG-1 Audio Layer 3 – The compression that saves valuable space while maintaining near-flawless quality of the original source of sound." });
            foramtList.Add(new Format() { Extension = ".m4a", Enabled = true, Description = "MPEG-4 Audio - An audio file format developed by Apple, designed to store high-quality sound efficiently." });
            foramtList.Add(new Format() { Extension = ".flac", Enabled = true, Description = "Free Lossless Audio Codec – This lossless audio format compresses audio data without losing any quality, making it perfect for preserving the original sound." });
            foramtList.Add(new Format() { Extension = ".alac", Enabled = false, Description = "Apple Lossless Audio Codec – Developed by Apple, this lossless audio format is designed for use on Apple devices, ensuring high-quality audio playback." });
            foramtList.Add(new Format() { Extension = ".wav", Enabled = false, Description = "Waveform Audio File Format – An uncompressed audio format that stores audio data in its raw waveform, offering pristine sound quality." });
            foramtList.Add(new Format() { Extension = ".wma", Enabled = false, Description = "Windows Media Audio – Windows audio format known for its lossless compression, retaining high audio quality throughout all types of restructuring processes." });
            foramtList.Add(new Format() { Extension = ".aac", Enabled = false, Description = "Advanced Audio Coding - An audio format that delivers decently high-quality sound and is enhanced using advanced coding." });
            foramtList.Add(new Format() { Extension = ".ogg", Enabled = false, Description = "Ogg Vorbis – An open-source digital multimedia container format designed to provide for efficient streaming and manipulation of digital multimedia." });
            foramtList.Add(new Format() { Extension = ".aiff", Enabled = false, Description = "Audio Interchange File Format – An uncompressed CD-quality audio format developed by Apple, commonly used in professional audio environments." });
        }
        AllFormats.AddRange(foramtList);
        ProtobufData.SaveToBin<FormatList>(DataFile.FormatsAllowed, new FormatList() { Formatlist = { foramtList } });

        var Description = "File exteniosns allowed for scanning tracks: ";
        foreach (var item in AllFormats)
            if (item.Enabled) Description += $"{item.Extension.Replace(".", "")}, ";

        FileExt.Description = Description.Remove(Description.Length - 2);
    }

    private void Ext_ToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        var toggle = sender as ToggleSwitch;
        if (toggle != null)
        {
            var formatUpdate = AllFormats.FirstOrDefault(e => e.Extension == toggle.Name);
            if (formatUpdate != null) formatUpdate.Enabled = toggle.IsOn;

            if (AllFormats.All(e => e.Enabled == false))
                GlobalNotification.Warning("At least one format must be enabled");

            ProtobufData.SaveToBin<FormatList>(DataFile.FormatsAllowed, new FormatList() { Formatlist = { AllFormats } });

            var Description = "File exteniosns allowed for scanning tracks: ";
            foreach (var item in AllFormats)
                if (item.Enabled) Description += $"{item.Extension.Replace(".", "")}, ";

            FileExt.Description = Description.Remove(Description.Length - 2);
        }
    }

    private void PlayPauseStopFadeSwitch_OnToggled(object sender, RoutedEventArgs? e)
    {
        var toggle = sender as ToggleSwitch;
        if (toggle != null)
            PlayPauseStopFadeSlider.IsEnabled = toggle.IsOn;
        else
            PlayPauseStopFadeSlider.IsEnabled = false;

        PlayPauseStopFadeSlider_OnIsEnabledChanged(PlayPauseStopFadeSwitch, null);
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PlayPauseStopFadeStatus)] = PlayPauseStopFadeSwitch.IsOn;
    }

    private void PlayPauseStopFadeSlider_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs? e)
    {

        PlayPauseStopFadeSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)]?.ToString() ?? 1000.ToString());
        PlayPauseStopFadeSlider_OnValueChanged(PlayPauseStopFadeSlider, null);
    }

    private void PlayPauseStopFadeSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs? e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)] = PlayPauseStopFadeSlider.Value;
        PlayPauseStopFadeCard.Description = PlayPauseStopFadeSlider.IsEnabled ? $"The music will fade in/out on Play/Pause/Stop. Fade Time: {PlayPauseStopFadeSlider.Value} ms" : "The music will not fade in/out on Play/Pause/Stop.";
    }

    private void AutoAdvanceSwitch_OnToggled(object sender, RoutedEventArgs? e)
    {
        var toggle = sender as ToggleSwitch;
        if (toggle != null)
            AutoAdvanceSlider.IsEnabled = toggle.IsOn;
        else
            AutoAdvanceSlider.IsEnabled = false;

        AutoAdvanceSlider_OnIsEnabledChanged(AutoAdvanceSwitch, null);
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoAdvanceStatus)] = AutoAdvanceSwitch.IsOn;
    }

    private void AutoAdvanceSlider_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs? e)
    {
        AutoAdvanceSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoAdvanceValue)]?.ToString() ?? 5000.ToString());
        AutoAdvanceSlider_OnValueChanged(AutoAdvanceSlider, null);
    }

    private void AutoAdvanceSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs? e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoAdvanceValue)] = AutoAdvanceSlider.Value;
        AutoAdvanceCard.Description = AutoAdvanceSlider.IsEnabled ? $"When the track ends and the next track starts automatically, the music will crossfade between tracks. Crossfade Time: {AutoAdvanceSlider.Value} ms" : "When the track ends and the next track starts automatically, the music will not crossfade between tracks.";
    }

    private void ManualTrackChangeSwitch_OnToggled(object sender, RoutedEventArgs? e)
    {
        var toggle = sender as ToggleSwitch;
        if (toggle != null)
            ManualTrackChangeSlider.IsEnabled = toggle.IsOn;
        else
            ManualTrackChangeSlider.IsEnabled = false;

        ManualTrackChangeSlider_OnIsEnabledChanged(ManualTrackChangeSwitch, null);
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ManualTrackChangeStatus)] = ManualTrackChangeSwitch.IsOn;
    }

    private void ManualTrackChangeSlider_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs? e)
    {
        ManualTrackChangeSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ManualTrackChangeValue)]?.ToString() ?? 2000.ToString());
        ManualTrackChangeSlider_OnValueChanged(ManualTrackChangeSlider, null);
    }

    private void ManualTrackChangeSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs? e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ManualTrackChangeValue)] = ManualTrackChangeSlider.Value;
        ManualTrackChangeCard.Description = ManualTrackChangeSlider.IsEnabled ? $"When you change the track manually, the music will crossfade between tracks. Crossfade Time: {ManualTrackChangeSlider.Value} ms" : "When you change the track manually, the music will not crossfade between tracks.";
    }

    private void PreviousReset_OnToggled(object sender, RoutedEventArgs e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PreviousResetStatus)] = PreviousReset.IsOn;
    }

    private void RestartTrackOnSelection_OnToggled(object sender, RoutedEventArgs e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RestartTrackOnSelectionStatus)] = RestartTrackOnSelection.IsOn;
    }

    private void UseSystemVolume_OnToggled(object sender, RoutedEventArgs e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.UseSystemVolumeStatus)] = UseSystemVolume.IsOn;
    }

    private void PauseOnMute_OnToggled(object sender, RoutedEventArgs e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PauseOnMuteStatus)] = PauseOnMute.IsOn;
    }

    private void AutoStart_OnToggled(object sender, RoutedEventArgs e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoStartStatus)] = AutoStart.IsOn;
    }

    private void MainPlayerBlurSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)] = MainPlayerBlurSlider.Value;
    }
}

