using Nucs.JsonSettings;
using Tunetastic.Models;
using System.Text;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace Tunetastic.Views;

public sealed partial class SettingsPage : Page
{
    LibrarySettings LibrarySaveSettings;

    public ObservableCollection<MusicLibraryPath> Libraries
    {
        get; set;
    } = new();
    public SettingViewModel ViewModel { get; }
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingViewModel>();
        this.InitializeComponent();
        LibrarySaveSettings = JsonSettings.Load<LibrarySettings>();
        numberBox.ValueChanged += NumberBox_ValueChanged;
        foreach (var lib in LibrarySaveSettings.LibraryPaths)
        {
            Libraries.Add(lib);
        }
        IgnoreDup.IsOn = LibrarySaveSettings.IgnoreEnabled;
        ScanAtStart.IsOn = LibrarySaveSettings.ScanAtStartup;

        IgnoreTrack.Description = $"Tracks are ignored if they are less than {LibrarySaveSettings.ignoreTracksBelowDuration} seconds";
        numberBox.Value = LibrarySaveSettings.ignoreTracksBelowDuration;
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

        foreach (var musicfolder in musicfolders)
        {
            var newFolder = new MusicLibraryPath
            {
                Name = musicfolder.Name,
                Path = musicfolder.Path
            };
            Libraries.Add(newFolder);
        }

        LibrarySaveSettings.LibraryPaths = Libraries.ToList();
        LibrarySaveSettings.Save();
    }

    private void RemoveFolder_ButtonClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;

        if (button!.CommandParameter is MusicLibraryPath path)
            Libraries.Remove(path);

        LibrarySaveSettings.LibraryPaths = Libraries.ToList();
        LibrarySaveSettings.Save();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ProgressRing.IsActive = true;
        ProgressRing.Visibility = Visibility.Visible;
        Scan.IsEnabled = false;
        await Task.Delay(1000);
        //await ViewModel.UpdateSongList();
        Scan.IsEnabled = true;
        ProgressRing.IsActive = false;
        ProgressRing.Visibility = Visibility.Collapsed;
        //Scan.Description set
    }

    private void IgnoreDup_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
            LibrarySaveSettings.IgnoreEnabled = toggleSwitch.IsOn;
        LibrarySaveSettings.Save();
    }

    private void ScanAtStart_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            LibrarySaveSettings.ScanAtStartup = toggleSwitch.IsOn;
        }
        LibrarySaveSettings.Save();
    }

    private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) => DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Normal, async () =>
    {
        var numberBox = sender as NumberBox;
        if (numberBox == null || numberBox.Value < 0)
        {
            numberBox.Value = 0;
        }
        IgnoreTrack.Description = $"Tracks are ignored if they are less than {numberBox.Value} seconds";
        
        LibrarySaveSettings.ignoreTracksBelowDuration = numberBox.Value;
        LibrarySaveSettings.Save();
    });
}

