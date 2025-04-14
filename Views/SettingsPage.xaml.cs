using Nucs.JsonSettings;
using Tunetastic.Models;
using System.Text;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using Tunetastic.Services;

namespace Tunetastic.Views;

public sealed partial class SettingsPage : Page
{
    public ObservableCollection<MusicLibraryPath> Libraries
    {
        get; set;
    } = new();
    public SettingViewModel ViewModel { get; }
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingViewModel>();
        this.InitializeComponent();
        
        numberBox.ValueChanged += NumberBox_ValueChanged;

        try
        {
            Libraries.AddRange(LibrarySettingsSaver.Instance.LibrarySaveSettings.LibraryPaths);
        }
        catch (Exception)
        {
        }
        IgnoreDup.IsOn = LibrarySettingsSaver.Instance.LibrarySaveSettings.IgnoreDuplicateEnabled;
        ScanAtStart.IsOn = LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanAtStartup;

        IgnoreTrack.Description = $"Tracks are ignored if they are less than {LibrarySettingsSaver.Instance.LibrarySaveSettings.ignoreTracksBelowDuration} seconds";
        numberBox.Value = LibrarySettingsSaver.Instance.LibrarySaveSettings.ignoreTracksBelowDuration;
        Scan.Description = LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult;
    }

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

    private async void AddNewFolder_ButtonClick(object sender, RoutedEventArgs e)
    {
        var Hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        var picker = new DevWinUI.FolderPicker(Hwnd);
        picker.Title = "Choose Library Folders";
        picker.CommitButtonText = "Add Folders";
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;

        var musicfolders = await picker.PickMultipleFoldersAsync();

        List<MusicLibraryPath> uniqueFolders = new();
        uniqueFolders.AddRange(Libraries);

        foreach (var musicfolder in musicfolders)
        {
            var newFolder = new MusicLibraryPath
            {
                Name = musicfolder.Name,
                Path = musicfolder.Path
            };
            uniqueFolders.Add(newFolder);
        }
        uniqueFolders = uniqueFolders.DistinctBy(p => p.Path).ToList();

        Libraries?.Clear();
        Libraries.AddRange(uniqueFolders);
        LibrarySettingsSaver.Instance.LibrarySaveSettings.LibraryPaths?.Clear();
        LibrarySettingsSaver.Instance.LibrarySaveSettings.LibraryPaths = Libraries.ToList();
        LibrarySettingsSaver.Instance.SaveSettings();
    }

    private void RemoveFolder_ButtonClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;

        if (button!.CommandParameter is MusicLibraryPath path)
            Libraries.Remove(path);

        LibrarySettingsSaver.Instance.LibrarySaveSettings.LibraryPaths?.Clear();
        LibrarySettingsSaver.Instance.LibrarySaveSettings.LibraryPaths = Libraries.ToList();
        LibrarySettingsSaver.Instance.SaveSettings();
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

        Scan.Description = LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanResult;
    }

    private void IgnoreDup_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
            LibrarySettingsSaver.Instance.LibrarySaveSettings.IgnoreDuplicateEnabled = toggleSwitch.IsOn;
        LibrarySettingsSaver.Instance.SaveSettings();
    }

    private void ScanAtStart_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            LibrarySettingsSaver.Instance.LibrarySaveSettings.ScanAtStartup = toggleSwitch.IsOn;
        }
        LibrarySettingsSaver.Instance.SaveSettings();
    }

    private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Normal, async () =>
    {
        var numberBox = sender as NumberBox;
        if (numberBox == null || numberBox.Value < 0)
        {
            numberBox.Value = 0;
        }
        IgnoreTrack.Description = $"Tracks are ignored if they are less than {numberBox.Value} seconds";

        LibrarySettingsSaver.Instance.LibrarySaveSettings.ignoreTracksBelowDuration = numberBox.Value;
        LibrarySettingsSaver.Instance.SaveSettings();
    });
}

