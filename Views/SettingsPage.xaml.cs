using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Tunetastic.Generated.Protos;
using Windows.UI;

namespace Tunetastic.Views;

/// <summary>
/// Represents the settings page in the application, allowing users to manage and modify various application settings.
/// </summary>
/// <remarks>
/// The <c>SettingsPage</c> is a UI page that provides options to configure application behaviors such as track preferences,
/// scanning options on startup, audio controls, and UI enhancements. It uses data binding and user interactions to reflect
/// changes dynamically in the application settings.
/// </remarks>
public sealed partial class SettingsPage : Page
{
	/// <summary>
	/// Represents a collection of music library directories stored as an ObservableCollection.
	/// </summary>
	/// <remarks>
	/// The Libraries property is used to manage and store the list of music libraries within
	/// the application. It allows binding to the UI for updates and manipulation of library data.
	/// This property supports adding, removing, and saving directories representing user-selected
	/// music folders. Folders are uniquely stored based on their Path property to avoid duplicates.
	/// Data for Libraries is initially loaded from a binary file serialized as a LibraryList object.
	/// Changes to the Libraries collection are also persisted back to the binary file.
	/// </remarks>
	public ObservableCollection<Library> Libraries
	{
		get; set;
	} = new();

	/// <summary>
	/// Represents a collection of file format configurations stored as an ObservableCollection.
	/// </summary>
	/// <remarks>
	/// The AllFormats property manages the list of supported file formats used for scanning tracks
	/// in the application. Each format is represented as a `Format` object, which includes
	/// properties such as file extension, enabled status, and description.
	/// This property is used to bind the list of formats to the UI for display and interaction.
	/// Updates to the formats, such as enabling or disabling specific formats, are reflected
	/// through this collection. The enabled formats are used to generate a dynamic description
	/// of allowed file extensions for file scanning tasks.
	/// Initially, the AllFormats collection is populated from a binary file storing a serialized
	/// `FormatList` object. Changes made to the collection are persisted back to the binary file
	/// to ensure consistency across application sessions.
	/// </remarks>
	public ObservableCollection<Format> AllFormats
	{
		get; set;
	} = new();

	public SettingViewModel ViewModel { get; }

	public SettingsPage()
	{
		ViewModel = App.GetService<SettingViewModel>();
		this.InitializeComponent();

		LoadAppearanceAndBehaviourSettings();

		Theme.SelectionChanged += Theme_SelectionChanged;
		Backdrop.SelectionChanged += Backdrop_SelectionChanged;
		numberBox.ValueChanged += NumberBox_ValueChanged;
		MainPlayerBlurSlider.ValueChanged += MainPlayerBlurSlider_OnValueChanged;
		RainbowSpeedSlider.ValueChanged += RainbowSpeedSlider_OnValueChanged;

		LoadLibrarySettings();

		LoadAudioAndPlayBackSettings();

		UpdateExtentionListOnUI();

		if (GetMusicDataService.IsScanning) ScanButton_Click(null, null);
	}

	/// <summary>
	/// Handles the event when the "Add New Folder" button is clicked.
	/// This method is responsible for initiating the process of creating a new folder
	/// in the application's user interface or file system as applicable.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Add New Folder" button.</param>
	/// <param name="e">An instance of EventArgs containing the event data.</param>
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

	/// <summary>
	/// Removes a folder from the list of user-specified libraries.
	/// This method is triggered when the "Remove Folder" button is clicked,
	/// and it updates the list of libraries and saves the updated list to a binary file.
	/// </summary>
	/// <param name="sender">The source of the event, typically a Button control.</param>
	/// <param name="e">The event data associated with the button click event.</param>
	private void RemoveFolder_ButtonClick(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;

		if (button!.CommandParameter is Library library)
			Libraries.Remove(library);

		ProtobufData.SaveToBin<LibraryList>(DataFile.AllLibraries, new LibraryList() { Libraries = { Libraries } });
	}

	/// <summary>
	/// Handles the click event for the Scan button.
	/// Triggers the process of scanning the music library, updates scan progress visuals
	/// in the UI, and ensures the button's state reflects the scanning activity.
	/// </summary>
	/// <param name="sender">The source of the event, generally the Scan button.</param>
	/// <param name="e">Event data associated with the button click event.</param>
	private async void ScanButton_Click(object? sender, RoutedEventArgs? e)
	{
		if (GetMusicDataService.IsScanning)
		{
			CustomProgressBar.Visibility = Visibility.Visible;
			Scan.IsEnabled = false;

			while (GetMusicDataService.IsScanning)
			{
				ProgressFill.Width = GetMusicDataService.ScanProgress * 2;
				ProgressFillText.Text = $"{GetMusicDataService.ScanProgress.ToString()}%";
				await Task.Delay(1);
			}

			for (double i = 1; i >= 0; i -= 0.02)
			{
				CustomProgressBar.Opacity = i;
				ProgressFillText.Opacity = i;
				await Task.Delay(1);
			}
			CustomProgressBar.Visibility = Visibility.Collapsed;
			Scan.IsEnabled = true;
			Scan.Description = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ScanResult)];
			return;
		}

		Scan.IsEnabled = false;
		ProgressFill.Width = 0;
		CustomProgressBar.Opacity = 0;
		ProgressFillText.Opacity = 0;
		ProgressFillText.Text = "0%";
		CustomProgressBar.Visibility = Visibility.Visible;

		_ = new GetMusicDataService().UpdateMetaData();

		for (double i = 0; i <= 1; i += 0.1)
		{
			CustomProgressBar.Opacity = i;
			ProgressFillText.Opacity = i;
			await Task.Delay(1);
		}

		while (GetMusicDataService.IsScanning)
		{
			ProgressFill.Width = GetMusicDataService.ScanProgress * 2;
			ProgressFillText.Text = $"{GetMusicDataService.ScanProgress.ToString()}%";
			await Task.Delay(1);
		}

		for (double i = 1; i >= 0; i -= 0.02)
		{
			CustomProgressBar.Opacity = i;
			ProgressFillText.Opacity = i;
			await Task.Delay(1);
		}
		CustomProgressBar.Visibility = Visibility.Collapsed;
		Scan.IsEnabled = true;
		Scan.Description = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ScanResult)];
	}

	/// <summary>
	/// Handles the toggled event for the "Ignore Duplicate Tracks" setting.
	/// This method updates the application's settings to enable or disable
	/// ignoring duplicate tracks based on the user's choice.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ToggleSwitch instance.</param>
	/// <param name="e">Event data containing information about the toggled event.</param>
	private void IgnoreDup_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)] = toggleSwitch.IsOn;
	}

	/// <summary>
	/// Handles the toggling action of the "Scan At Startup" setting.
	/// Updates the application's library settings to enable or disable
	/// scanning for tracks in the library during application startup.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ToggleSwitch control.</param>
	/// <param name="e">Event data that provides information about the toggle action.</param>
	private void ScanAtStart_Toggled(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ScanAtStartup)] = toggleSwitch.IsOn;
	}

	/// <summary>
	/// Handles the event triggered when the value of the NumberBox changes.
	/// This method is used to process or respond to the updated value entered by the user.
	/// </summary>
	/// <param name="sender">The source of the event, typically the NumberBox control.</param>
	/// <param name="e">An object containing the event data, which provides details about the value change.</param>
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

	/// <summary>
	/// Updates the list of file format extensions on the user interface.
	/// This method loads the list of allowed file formats from a binary file,
	/// updates the internal data structure, and refreshes the UI to reflect
	/// the currently enabled file extension settings.
	/// If no file extensions are enabled, an appropriate message is set
	/// in the description.
	/// </summary>
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
		AllFormats.Clear();
		AllFormats.AddRange(foramtList);
		ProtobufData.SaveToBin<FormatList>(DataFile.FormatsAllowed, new FormatList() { Formatlist = { foramtList } });

		var enabledExtensions = AllFormats
			.Where(f => f.Enabled)
			.Select(f => f.Extension.TrimStart('.')).ToList();

		var description = enabledExtensions.Any()
			? $"File extensions allowed for scanning tracks: {string.Join(", ", enabledExtensions)}"
			: "No file extensions enabled for scanning tracks";

		FileExt.Description = description;
	}

	/// <summary>
	/// Handles the toggled event for the file extension toggle switches on the settings page.
	/// This method updates the enabled state of the corresponding file format, ensures at least one format is enabled,
	/// updates the global description, and saves the updated list of enabled file formats to the binary data file.
	/// </summary>
	/// <param name="sender">The toggle switch that triggered the event.</param>
	/// <param name="e">Event data providing context for the toggled event.</param>
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

			var enabledExtensions = AllFormats
				.Where(f => f.Enabled)
				.Select(f => f.Extension.TrimStart('.')).ToList();

			var description = enabledExtensions.Any()
				? $"File extensions allowed for scanning tracks: {string.Join(", ", enabledExtensions)}"
				: "No file extensions enabled for scanning tracks";

			FileExt.Description = description;
		}
	}

	/// <summary>
	/// Handles the toggled event for the Play/Pause/Stop Fade switch.
	/// This method updates the enabled state of the related fade slider
	/// and persists the toggle switch's state in the application settings.
	/// </summary>
	/// <param name="sender">The control that triggered the toggled event, typically a ToggleSwitch.</param>
	/// <param name="e">The event data for the toggled event, which may be null in some cases.</param>
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

	/// <summary>
	/// Handles the event when the IsEnabled property of the PlayPauseStopFadeSlider control changes.
	/// Updates the slider value based on the persisted application settings and triggers the value changed event for further processing.
	/// </summary>
	/// <param name="sender">The source of the event, typically the PlayPauseStopFadeSlider control.</param>
	/// <param name="e">Event arguments containing details of the IsEnabled property change.</param>
	private void PlayPauseStopFadeSlider_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs? e)
	{
		PlayPauseStopFadeSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)]?.ToString() ?? 1000.ToString());
		PlayPauseStopFadeSlider_OnValueChanged(PlayPauseStopFadeSlider, null);
	}

	/// <summary>
	/// Handles the ValueChanged event for the PlayPauseStopFadeSlider.
	/// Saves the current slider value to local settings and updates the description of the
	/// PlayPauseStopFadeCard to reflect the fade time and its enablement status.
	/// </summary>
	/// <param name="sender">The slider control that triggered the event.</param>
	/// <param name="e">The event data containing the old and new values of the slider.</param>
	private void PlayPauseStopFadeSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs? e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)] = PlayPauseStopFadeSlider.Value;
		PlayPauseStopFadeCard.Description = PlayPauseStopFadeSlider.IsEnabled ? $"The music will fade in/out on Play/Pause/Stop. Fade Time: {PlayPauseStopFadeSlider.Value} ms" : "The music will not fade in/out on Play/Pause/Stop.";
	}

	/// <summary>
	/// Handles the toggled event for the AutoAdvanceSwitch ToggleSwitch control.
	/// Adjusts the IsEnabled state of the associated AutoAdvanceSlider and updates
	/// the application's local settings to preserve the AutoAdvance status.
	/// </summary>
	/// <param name="sender">
	/// The source of the event, typically the AutoAdvanceSwitch ToggleSwitch control.
	/// </param>
	/// <param name="e">
	/// Event data that can provide additional information about the toggle action.
	/// </param>
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

	/// <summary>
	/// Handles the event triggered when the <see cref="AutoAdvanceSlider"/> control's IsEnabled state changes.
	/// Updates the slider value to reflect the currently saved value for auto-advance timing and triggers the value change handling logic.
	/// </summary>
	/// <param name="sender">
	/// The source object of the event, generally the <see cref="AutoAdvanceSlider"/> control.
	/// </param>
	/// <param name="e">
	/// Contains the event data associated with the change in the IsEnabled property. Can be null if not provided.
	/// </param>
	private void AutoAdvanceSlider_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs? e)
	{
		AutoAdvanceSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoAdvanceValue)]?.ToString() ?? 5000.ToString());
		AutoAdvanceSlider_OnValueChanged(AutoAdvanceSlider, null);
	}

	/// <summary>
	/// Handles the ValueChanged event for the AutoAdvanceSlider control.
	/// Updates the application's local settings with the new crossfade duration
	/// and sets the description text of the AutoAdvanceCard accordingly.
	/// </summary>
	/// <param name="sender">The source of the event, typically the AutoAdvanceSlider control.</param>
	/// <param name="e">The event data that provides information about the old and new values of the slider.</param>
	private void AutoAdvanceSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs? e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoAdvanceValue)] = AutoAdvanceSlider.Value;
		AutoAdvanceCard.Description = AutoAdvanceSlider.IsEnabled ? $"When the track ends and the next track starts automatically, the music will crossfade between tracks. Crossfade Time: {AutoAdvanceSlider.Value} ms" : "When the track ends and the next track starts automatically, the music will not crossfade between tracks.";
	}

	/// <summary>
	/// Handles the Toggled event for the ManualTrackChangeSwitch toggle switch.
	/// This method updates the enabled state of the ManualTrackChangeSlider
	/// based on the toggle switch's state and persists the new state in the application's local settings.
	/// </summary>
	/// <param name="sender">The object that raised the event.</param>
	/// <param name="e">The event data associated with the Toggled event. Can be null.</param>
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

	/// <summary>
	/// Handles the event when the <see cref="ManualTrackChangeSlider"/>'s IsEnabled property changes.
	/// This method updates the value of the slider and triggers the appropriate value change logic.
	/// </summary>
	/// <param name="sender">The source object of the event, typically the <see cref="ManualTrackChangeSlider"/>.</param>
	/// <param name="e">Event data that contains information about the dependency property change, or null if not provided.</param>
	private void ManualTrackChangeSlider_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs? e)
	{
		ManualTrackChangeSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ManualTrackChangeValue)]?.ToString() ?? 2000.ToString());
		ManualTrackChangeSlider_OnValueChanged(ManualTrackChangeSlider, null);
	}

	/// <summary>
	/// Handles the event when the value of the Manual Track Change slider changes.
	/// Updates the stored application settings to reflect the new slider value and
	/// adjusts the description of the associated settings card to indicate the effect
	/// of the selected crossfade time or the disabled state.
	/// </summary>
	/// <param name="sender">The slider control that raised the ValueChanged event.</param>
	/// <param name="e">The event data associated with the value change, or null if unavailable.</param>
	private void ManualTrackChangeSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs? e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ManualTrackChangeValue)] = ManualTrackChangeSlider.Value;
		ManualTrackChangeCard.Description = ManualTrackChangeSlider.IsEnabled ? $"When you change the track manually, the music will crossfade between tracks. Crossfade Time: {ManualTrackChangeSlider.Value} ms" : "When you change the track manually, the music will not crossfade between tracks.";
	}

	/// <summary>
	/// Handles the toggled event for the "Previous button resets the current track" setting.
	/// Updates the application's local settings to reflect the user's preference for whether
	/// the previous button resets the track or not.
	/// </summary>
	/// <param name="sender">The source of the event, typically a ToggleSwitch.</param>
	/// <param name="e">Event data associated with the toggled event.</param>
	private void PreviousReset_OnToggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PreviousResetStatus)] = PreviousReset.IsOn;
	}

	/// <summary>
	/// Handles the toggled event for the "Restart Track on Selection" setting.
	/// Updates the local settings to reflect the current state of the toggle switch,
	/// which determines whether the current track restarts when selected again.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ToggleSwitch.</param>
	/// <param name="e">Event data that provides additional context for the toggle action.</param>
	private void RestartTrackOnSelection_OnToggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RestartTrackOnSelectionStatus)] = RestartTrackOnSelection.IsOn;
	}

	/// <summary>
	/// Handles the toggled event for the `Use System Volume` toggle switch.
	/// Updates the local settings to reflect the current status of the `Use System Volume` option,
	/// allowing the application to either use or bypass the system's volume control for playback management.
	/// </summary>
	/// <param name="sender">The source of the event, typically the `ToggleSwitch` that was toggled.</param>
	/// <param name="e">Event data that provides information about the toggle event.</param>
	private void UseSystemVolume_OnToggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.UseSystemVolumeStatus)] = UseSystemVolume.IsOn;
		//TODO: Add logic to handle the use system volume setting
	}

	/// <summary>
	/// Handles the toggled event for the "Pause on Mute" setting.
	/// Updates the application's local settings to reflect whether the track should
	/// be paused when the system volume is muted.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ToggleSwitch control.</param>
	/// <param name="e">The event data associated with the toggle action.</param>
	private void PauseOnMute_OnToggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PauseOnMuteStatus)] = PauseOnMute.IsOn;
		//TODO: Add logic to handle the pause on mute setting
	}

	/// <summary>
	/// Handles the toggled event for the AutoStart toggle switch.
	/// Updates the local settings to reflect the current state of the AutoStart toggle,
	/// determining whether the application should automatically start playing the last track
	/// when the app is launched.
	/// </summary>
	/// <param name="sender">The source of the event, typically the control that triggered the event.</param>
	/// <param name="e">Contains event data specific to the toggled action.</param>
	private void AutoStart_OnToggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.AutoStartStatus)] = AutoStart.IsOn;
	}

	/// <summary>
	/// Handles the event triggered when the value of the MainPlayerBlurSlider changes.
	/// Updates the main player's background blur value in the application's local settings
	/// with the new slider value.
	/// </summary>
	/// <param name="sender">The object that raised the event, typically the slider control.</param>
	/// <param name="e">The event data containing information about the old and new values of the slider.</param>
	private void MainPlayerBlurSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)] = MainPlayerBlurSlider.Value;
	}

	/// <summary>
	/// Handles the event triggered when the selection in the Theme ComboBox changes.
	/// Updates the application's theme based on the selected option and persists the
	/// chosen theme to local settings for future sessions.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ComboBox object.</param>
	/// <param name="e">The event arguments containing details about the selection change.</param>
	private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		App.Current.ThemeService.OnThemeComboBoxSelectionChanged(sender);
		if (Theme.SelectedItem is ComboBoxItem ThemeItem)
		{
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.Theme)] = ThemeItem.Tag?.ToString();
		}
		App.Current.ThemeService.UpdateCaptionButtons();

		if (TintSettings.Visibility == Visibility.Visible)
		{
			var actualTheme = App.Current.ThemeService.GetActualTheme();
			Color color = Color.FromArgb(0, 0, 0, 0);
			if (actualTheme == ElementTheme.Light)
				color = Color.FromArgb(255, 223, 223, 223);
			else if (actualTheme == ElementTheme.Dark)
				color = Color.FromArgb(255, 32, 32, 32);

			TintBox.Fill = new SolidColorBrush(color);
		}
	}

	/// <summary>
	/// Handles the event triggered when the backdrop selection is changed in the settings page.
	/// Updates the application's theme service and saves the selected backdrop option to local settings.
	/// </summary>
	/// <param name="sender">The source of the event, typically the ComboBox control.</param>
	/// <param name="e">The event data containing information about the selection change.</param>
	private void Backdrop_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		App.Current.ThemeService.OnBackdropComboBoxSelectionChanged(sender);
		if (Backdrop.SelectedItem is ComboBoxItem BackdropItem)
		{
			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			localSettings.Values[nameof(LocalSave.Backdrop)] = BackdropItem.Tag?.ToString();

			TintSettings.Visibility = BackdropItem.Tag?.ToString() == "Mica" ? Visibility.Visible : Visibility.Collapsed;

			if (TintSettings.Visibility == Visibility.Visible)
			{
				var actualTheme = App.Current.ThemeService.GetActualTheme();
				Color color = Color.FromArgb(0, 0, 0, 0);
				if (actualTheme == ElementTheme.Light)
					color = Color.FromArgb(255, 223, 223, 223);
				else if (actualTheme == ElementTheme.Dark)
					color = Color.FromArgb(255, 32, 32, 32);

				TintBox.Fill = new SolidColorBrush(color);
			}

			localSettings.Values[nameof(LocalSave.BackdropTintColorStatus)] = false.ToString();
			localSettings.Values.Remove(nameof(LocalSave.BackdropTintColorA));
			localSettings.Values.Remove(nameof(LocalSave.BackdropTintColorR));
			localSettings.Values.Remove(nameof(LocalSave.BackdropTintColorG));
			localSettings.Values.Remove(nameof(LocalSave.BackdropTintColorB));
		}
	}

	/// <summary>
	/// Handles the event when the color is changed on the ColorPicker.
	/// Updates the application UI tint, persists the selected color to local settings,
	/// and integrates the new color into the application's theme service.
	/// </summary>
	/// <param name="sender">The ColorPicker control triggering the event.</param>
	/// <param name="args">An instance of ColorChangedEventArgs containing the new color information.</param>
	private void OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
	{
		ApplyAndSaveTint(args.NewColor);
	}

	/// <summary>
	/// Handles the event when a color palette item is clicked.
	/// This method updates the application's theme and tint settings based on the selected color
	/// and modifies the UI accordingly.
	/// </summary>
	/// <param name="sender">The source of the event, typically the color palette control.</param>
	/// <param name="e">An instance of ItemClickEventArgs containing information about the clicked item, including the selected color.</param>
	private void OnColorPaletteItemClick(object sender, ItemClickEventArgs e)
	{
		var color = e.ClickedItem as ColorPaletteItem;
		if (color != null)
			ApplyAndSaveTint(color.Color);
	}

	/// <summary>
	/// Applies the specified tint color to the application's background and updates the UI reflecting the new color.
	/// Additionally, the tint color values are saved into the application's local settings for persistence across sessions.
	/// </summary>
	/// <param name="color">The color to be applied as the background tint.</param>
	private void ApplyAndSaveTint(Color color)
	{
		App.Current.ThemeService.SetBackdropTintColor(color);
		TintBox.Fill = new SolidColorBrush(color);
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.BackdropTintColorStatus)] = true.ToString();
		localSettings.Values[nameof(LocalSave.BackdropTintColorA)] = color.A.ToString();
		localSettings.Values[nameof(LocalSave.BackdropTintColorR)] = color.R.ToString();
		localSettings.Values[nameof(LocalSave.BackdropTintColorG)] = color.G.ToString();
		localSettings.Values[nameof(LocalSave.BackdropTintColorB)] = color.B.ToString();
	}

	/// <summary>
	/// Loads and applies the appearance and behavior settings for the application.
	/// This includes configuring the theme, backdrop, tint color, and additional visual
	/// elements such as background blur values. The method retrieves these settings
	/// from local storage and applies them to the user interface elements accordingly.
	/// </summary>
	private void LoadAppearanceAndBehaviourSettings()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		Theme.SelectedItem = Theme.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Tag?.ToString() == (localSettings.Values[nameof(LocalSave.Theme)]?.ToString() ?? "Default"));
		var backdrop = (localSettings.Values[nameof(LocalSave.Backdrop)]?.ToString() ?? "DesktopAcrylic");
		Backdrop.SelectedItem = Backdrop.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Tag?.ToString() == backdrop);

		if (backdrop == "Mica")
		{
			TintSettings.Visibility = Visibility.Visible;
			Color color = Color.FromArgb(0, 0, 0, 0);
			if (bool.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorStatus)]?.ToString() ?? "false"))
			{
				color = Color.FromArgb(a: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorA)]?.ToString() ?? "255"),
										   r: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorR)]?.ToString() ?? "32"),
										   g: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorG)]?.ToString() ?? "32"),
										   b: byte.Parse(localSettings.Values[nameof(LocalSave.BackdropTintColorB)]?.ToString() ?? "32"));
				App.Current.ThemeService.SetBackdropTintColor(color);
			}
			else
			{
				var actualTheme = App.Current.ThemeService.GetActualTheme();
				if (actualTheme == ElementTheme.Light)
					color = Color.FromArgb(255, 223, 223, 223);
				else if (actualTheme == ElementTheme.Dark)
					color = Color.FromArgb(255, 32, 32, 32);
			}
			TintBox.Fill = new SolidColorBrush(color);
		}
		else
			TintSettings.Visibility = Visibility.Collapsed;

		MainPlayerBlurSlider.Value = int.Parse(localSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)]?.ToString() ?? 5.ToString());
		RainbowToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false");
		RainbowToggle_OnToggled(RainbowToggle, null);
	}

	/// <summary>
	/// Loads and applies the library-related settings for the application.
	/// This includes retrieving the list of libraries, loading toggle states and values
	/// for options such as ignoring duplicate tracks and scanning libraries at startup,
	/// and updating UI components to reflect these settings.
	/// </summary>
	private void LoadLibrarySettings()
	{
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
	}

	/// <summary>
	/// Loads the current audio and playback settings from the application's local storage
	/// and updates the corresponding UI elements with the retrieved values.
	/// This method initializes settings such as play/pause fade behavior, auto-advance playback,
	/// manual track change, resetting previous track, and other related preferences.
	/// </summary>
	private void LoadAudioAndPlayBackSettings()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

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
	}

	/// <summary>
	/// Handles the event triggered when the "RainbowToggle" is toggled.
	/// Manages the enabling or disabling of the rainbow frame feature in the application and updates UI components
	/// accordingly. Also persists the toggle state in application settings.
	/// </summary>
	/// <param name="sender">The source of the event, expected to be the "RainbowToggle" control.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data, can be null.</param>
	private void RainbowToggle_OnToggled(object sender, RoutedEventArgs? e)
	{
		var toggle = sender as ToggleSwitch;
		if (toggle != null)
		{
			if (toggle.IsOn)
			{
				App.Current.RainbowFrame.StartRainbowFrame();
				RainbowOnlyDuringPlayback.IsOn = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)]?.ToString() ?? "false");
				RainbowOnlyDuringPlayback_OnToggled(RainbowOnlyDuringPlayback, null);
				RainbowSpeedSlider.IsEnabled = true;
				RainbowOnlyDuringPlayback.IsEnabled = true;
			}
			else
			{
				App.Current.RainbowFrame.StopRainbowFrame();
				App.Current.RainbowFrame.ResetFrameColorToDefault();
				RainbowSpeedSlider.IsEnabled = false;
				RainbowOnlyDuringPlayback.IsEnabled = false;
			}
			RainbowSpeedSlider_OnIsEnabledChanged(RainbowSpeedSlider, null);
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RainbowFrameStatus)] = toggle.IsOn.ToString();
		}
	}

	/// <summary>
	/// Handles the toggle event for the "Rainbow Only During Playback" feature.
	/// This method manages the activation or deactivation of the rainbow frame effect
	/// based on the toggle state and updates the corresponding application settings.
	/// </summary>
	/// <param name="sender">The source of the event, typically a toggle switch control.</param>
	/// <param name="e">An instance of <see cref="RoutedEventArgs"/> containing the event data, can be null.</param>
	private void RainbowOnlyDuringPlayback_OnToggled(object sender, RoutedEventArgs? e)
	{
		var toggle = sender as ToggleSwitch;
		if (toggle != null)
		{
			if (toggle.IsOn)
			{
				App.Current.RainbowFrame.StopRainbowFrame();
				App.Current.RainbowFrame.ResetFrameColorToDefault();
			}
			else App.Current.RainbowFrame.StartRainbowFrame();

			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RainbowOnlyDuringPlayback)] = toggle.IsOn.ToString();
		}
	}

	/// <summary>
	/// Handles the event when the "IsEnabled" property of the RainbowSpeedSlider changes.
	/// This method ensures the slider's value is updated based on the saved application settings
	/// and triggers further updates to reflect the change.
	/// </summary>
	/// <param name="sender">The source of the event, typically the RainbowSpeedSlider.</param>
	/// <param name="e">An instance of DependencyPropertyChangedEventArgs containing details about the property change, or null if not provided.</param>
	private void RainbowSpeedSlider_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs? e)
	{
		RainbowSpeedSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RainbowFrameSpeed)]?.ToString() ?? "31");
		RainbowSpeedSlider_OnValueChanged(RainbowSpeedSlider, null);
	}

	/// <summary>
	/// Handles the ValueChanged event for the RainbowSpeedSlider slider control.
	/// Updates the application settings with the new slider value and modifies the description
	/// text and visual effect speed accordingly.
	/// </summary>
	/// <param name="sender">The source of the event, typically the RainbowSpeedSlider control.</param>
	/// <param name="e">An instance of RangeBaseValueChangedEventArgs containing the event data, including the old and new values of the slider.</param>
	private void RainbowSpeedSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs? e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.RainbowFrameSpeed)] = RainbowSpeedSlider.Value.ToString();
		RainbowSpeed.Description = RainbowSpeedSlider.IsEnabled ? $"Current Rainbow frame border Speed: {RainbowSpeedSlider.Value}" : "Rainbow frame border is disabled";
		if (RainbowSpeedSlider.IsEnabled)
		{
			var effectSpeed = int.Parse(RainbowSpeedSlider.Value.ToString());

			App.Current.RainbowFrame.UpdateEffectSpeed(51 - effectSpeed);
		}
	}
}

