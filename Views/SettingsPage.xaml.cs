using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using Windows.Services.Store;
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
	public ObservableCollection<LibraryModel> Libraries
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
	public ObservableCollection<MusicFormatModel> AllFormats
	{
		get; set;
	} = new();

	public SettingViewModel ViewModel { get; }

	public SettingsPage()
	{
		ViewModel = App.GetService<SettingViewModel>();
		this.InitializeComponent();

		LoadAppearanceAndBehaviourSettings();

		LoadLibrarySettings();

		LoadAudioAndPlayBackSettings();

		UpdateExtentionListOnUI();

		LoadAboutSectionSettings();

		Theme.SelectionChanged += Theme_SelectionChanged;
		Backdrop.SelectionChanged += Backdrop_SelectionChanged;
		IgnoretracksDuration.ValueChanged += NumberBox_ValueChanged;
		MainPlayerBlurSlider.ValueChanged += MainPlayerBlurSlider_OnValueChanged;
		RainbowSpeedSlider.ValueChanged += RainbowSpeedSlider_OnValueChanged;
		PlayPauseStopFadeSlider.ValueChanged += PlayPauseStopFadeSlider_OnValueChanged;
		ArtistsToggle.Toggled += ArtistsToggle_Toggled;
		AlbumsToggle.Toggled += AlbumsToggle_Toggled;
		GenresToggle.Toggled += GenresToggle_Toggled;
		YearsToggle.Toggled += YearsToggle_Toggled;
		RecentlyAddedToggle.Toggled += RecentlyAddedToggle_Toggled;
		RecentlyPlayedToggle.Toggled += RecentlyPlayedToggle_Toggled;
		MostPlayedToggle.Toggled += MostPlayedToggle_Toggled;
		#region Uncomment when crossfade is implemented properly
		//AutoAdvanceSlider.ValueChanged += AutoAdvanceSlider_OnValueChanged;
		//ManualTrackChangeSlider.ValueChanged += ManualTrackChangeSlider_OnValueChanged;
		#endregion

		if (GetMusicData.IsScanning) ScanButton_Click(null, null);
		Page_ActualThemeChanged(null, null);
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
		var picker = new FolderPicker((sender as Button).XamlRoot.ContentIslandEnvironment.AppWindowId);
		picker.CommitButtonText = "Add Folder";
		picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

		try
		{
			var musicfolder = await picker.PickSingleFolderAsync();

			var libraryModel = new LibraryModel
			{
				Name = Path.GetFileName(musicfolder.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
				Path = musicfolder.Path
			};

			await DatabaseHelper.Instance.AddOrUpdateLibrary(libraryModel);

			Libraries?.Clear();
			Libraries.AddRange(await DatabaseHelper.Instance.GetAllLibraries());
		}
		catch (Exception)
		{
			GlobalNotification.Error("Error Adding the folder. Make sure the folder path doesn't contain any shortcuts. It's better to provide complete folder path.");
		}
	}

	/// <summary>
	/// Removes a folder from the list of user-specified libraries.
	/// This method is triggered when the "Remove Folder" button is clicked,
	/// and it updates the list of libraries and saves the updated list to a binary file.
	/// </summary>
	/// <param name="sender">The source of the event, typically a Button control.</param>
	/// <param name="e">The event data associated with the button click event.</param>
	private async void RemoveFolder_ButtonClick(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;

		if (button!.CommandParameter is LibraryModel library)
		{
			await DatabaseHelper.Instance.RemoveLibrary(library);
			Libraries.Remove(library);
		}
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
		if (GetMusicData.IsScanning)
		{
			CustomProgressBar.Visibility = Visibility.Visible;
			Scan.IsEnabled = false;

			while (GetMusicData.IsScanning)
			{
				ProgressFill.Width = GetMusicData.ScanProgress * 2;
				ProgressFillText.Text = $"{GetMusicData.ScanProgress.ToString()}%";
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
		var pendingTasks = await DatabaseHelper.Instance.GetAllPendingTagWrites();
		if (pendingTasks.Count > 0)
		{
			var dialog = new ContentDialog
			{
				Title = "Scan Libraries",
				PrimaryButtonText = "Continue",
				SecondaryButtonText = "Cancel",
				DefaultButton = ContentDialogButton.Primary,
				Background = (Brush)Application.Current.Resources["AcrylicBackgroundFillColorBaseBrush"],

				Content = new Grid
				{
					Children =
					{
						new TextBlock
						{
							Text = $"You have pending tag writes to {pendingTasks.Count} of the file{(pendingTasks.Count > 1 ? "s" : "")}. If you continue, they will be back to original state.\n\nDo you want to continue?",
							TextWrapping = TextWrapping.WrapWholeWords
						}
					}
				},
				XamlRoot = this.Content.XamlRoot
			};
			MainWindow._instance.WindowResizePermission(false);
			var result = await dialog.ShowAsync();
			MainWindow._instance.WindowResizePermission(true);

			if (result != ContentDialogResult.Primary)
			{
				return;
			}
		}

		Scan.IsEnabled = false;
		ProgressFill.Width = 0;
		CustomProgressBar.Opacity = 0;
		ProgressFillText.Opacity = 0;
		ProgressFillText.Text = "0%";
		CustomProgressBar.Visibility = Visibility.Visible;

		_ = new GetMusicData().UpdateMetaData();

		for (double i = 0; i <= 1; i += 0.1)
		{
			CustomProgressBar.Opacity = i;
			ProgressFillText.Opacity = i;
			await Task.Delay(1);
		}

		while (GetMusicData.IsScanning)
		{
			ProgressFill.Width = GetMusicData.ScanProgress * 2;
			ProgressFillText.Text = $"{GetMusicData.ScanProgress.ToString()}%";
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

		if (numberBox?.Value < 0 || double.IsNaN(numberBox.Value))
		{
			numberBox.Value = 0;
		}
		IgnoreTrack.Description = $"Tracks are ignored if they are less than {numberBox?.Value} seconds";

		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)] = numberBox?.Value;
	});

	/// <summary>
	/// Updates the list of file format extensions displayed on the user interface.
	/// This method retrieves the list of supported music file formats from the database,
	/// updates the internal collection of formats, and refreshes the UI description to
	/// reflect the enabled file extensions. If no file extensions are enabled, a default
	/// message indicating this is displayed on the UI.
	/// </summary>
	private async void UpdateExtentionListOnUI()
	{
		var formatList = await DatabaseHelper.Instance.GetAllMusicFormats();

		AllFormats.Clear();
		AllFormats.AddRange(formatList);

		var enabledExtensions = AllFormats.Where(f => f.Enabled).Select(f => f.Extension.TrimStart('.')).ToList();

		var description = enabledExtensions.Any() ? $"File extensions allowed for scanning tracks: {string.Join(", ", enabledExtensions)}" : "No file extensions enabled for scanning tracks";

		FileExt.Description = description;
	}

	/// <summary>
	/// Handles the toggled event for the file extension toggle switches on the settings page.
	/// This method updates the state of the relevant file format, ensures that at least one
	/// format is enabled, updates the global description, and persists the changes to the binary data file.
	/// </summary>
	/// <param name="sender">The source of the event, representing the toggle switch being toggled.</param>
	/// <param name="e">Event data that provides information about the toggled event.</param>
	private async void Ext_ToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
	{
		var toggle = sender as ToggleSwitch;
		if (toggle != null)
		{
			await DatabaseHelper.Instance.SetMusicFormatEnabled(extension: toggle.Name, enabled: toggle.IsOn);

			if (AllFormats.All(e => e.Enabled == false))
				GlobalNotification.Warning("At least one format must be enabled");

			var enabledExtensions = AllFormats.Where(f => f.Enabled).Select(f => f.Extension.TrimStart('.')).ToList();

			var description = enabledExtensions.Any() ? $"File extensions allowed for scanning tracks: {string.Join(", ", enabledExtensions)}" : "No file extensions enabled for scanning tracks";

			FileExt.Description = description;
			//TODO: live update without scan
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
		PlayPauseStopFadeSlider.Value = int.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.PlayPauseStopFadeValue)]?.ToString() ?? 700.ToString());
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
		AutoAdvanceSlider.IsEnabled = toggle != null && toggle.IsOn;

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
		ManualTrackChangeSlider.IsEnabled = toggle != null && toggle.IsOn;

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
		if (UseSystemVolume.IsOn)
			MainPage._instance?.SwitchToSystemVolumeSliderControl();
		else
			MainPage._instance?.SwitchToAppVolumeSliderControl();
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
	private async void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		App.Current.ThemeService.OnThemeComboBoxSelectionChanged(sender);
		if (Theme.SelectedItem is ComboBoxItem ThemeItem)
		{
			Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.Theme)] = ThemeItem.Tag?.ToString();
		}

		if (TintSettings.Visibility == Visibility.Visible)
		{
			var actualTheme = App.Current.ThemeService.ActualTheme;
			Color color = actualTheme switch
			{
				ElementTheme.Light => Color.FromArgb(255, 223, 223, 223),
				ElementTheme.Dark => Color.FromArgb(255, 32, 32, 32),
				_ => Color.FromArgb(0, 0, 0, 0)
			};

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
				var actualTheme = App.Current.ThemeService.ActualTheme;
				Color color = Color.FromArgb(0, 0, 0, 0);
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
	private void ColorPalette_ColorChanged(object sender, ColorPaletteColorChangedEventArgs e)
	{
		var color = e.ColorPaletteItem;
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
		App.Current.ThemeService.GetMicaSystemBackdrop().TintColor = color;
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
		var backdrop = (localSettings.Values[nameof(LocalSave.Backdrop)]?.ToString() ?? "Acrylic");
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
				App.Current.ThemeService.GetMicaSystemBackdrop().TintColor = color;
			}
			TintBox.Fill = new SolidColorBrush(color);
		}
		else
			TintSettings.Visibility = Visibility.Collapsed;

		MainPlayerBlurSlider.Value = int.Parse(localSettings.Values[nameof(LocalSave.MainPlayerBGBlurValue)]?.ToString() ?? 5.ToString());
		RainbowToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.RainbowFrameStatus)]?.ToString() ?? "false");
		RainbowToggle_OnToggled(RainbowToggle, null);
		MinimizeToTray.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.MinimizeToTray)]?.ToString() ?? "true");
	}

	/// <summary>
	/// Loads and applies the library-related settings for the application.
	/// This includes retrieving the list of libraries, loading toggle states and values
	/// for options such as ignoring duplicate tracks and scanning libraries at startup,
	/// and updating UI components to reflect these settings.
	/// </summary>
	private async Task LoadLibrarySettings()
	{
		try
		{
			Libraries.AddRange(await DatabaseHelper.Instance.GetAllLibraries());
		}
		catch (Exception)
		{
			// ignored
		}

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

		IgnoreDup.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.IgnoreDuplicateEnabled)]?.ToString() ?? "false");

		ScanAtStart.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.ScanAtStartup)]?.ToString() ?? "false");

		IgnoreTrack.Description = $"Tracks are ignored if they are less than {localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0"} seconds";

		IgnoretracksDuration.Value = double.Parse(localSettings.Values[nameof(LocalSave.IgnoreTracksBelowDuration)]?.ToString() ?? "0");

		Scan.Description = localSettings.Values[nameof(LocalSave.ScanResult)];

		ArtistsToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.ArtistsEnabled)]?.ToString() ?? "true");

		AlbumsToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.AlbumsEnabled)]?.ToString() ?? "true");

		GenresToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.GenresEnabled)]?.ToString() ?? "true");

		YearsToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.YearsEnabled)]?.ToString() ?? "true");

		RecentlyAddedToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.RecentlyAddedEnabled)]?.ToString() ?? "true");

		RecentlyPlayedToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.RecentlyPlayedEnabled)]?.ToString() ?? "true");

		MostPlayedToggle.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.MostPlayedEnabled)]?.ToString() ?? "true");
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

		#region Uncomment when crossfade is implemented properly
		/*AutoAdvanceSwitch.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.AutoAdvanceStatus)]?.ToString() ?? "false");
		AutoAdvanceSwitch_OnToggled(AutoAdvanceSwitch, null);

		ManualTrackChangeSwitch.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.ManualTrackChangeStatus)]?.ToString() ?? "false");
		ManualTrackChangeSwitch_OnToggled(ManualTrackChangeSwitch, null);*/
		#endregion

		PreviousReset.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.PreviousResetStatus)]?.ToString() ?? "false");

		RestartTrackOnSelection.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.RestartTrackOnSelectionStatus)]?.ToString() ?? "true");

		UseSystemVolume.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.UseSystemVolumeStatus)]?.ToString() ?? "true");

		PauseOnMute.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.PauseOnMuteStatus)]?.ToString() ?? "true");

		AutoStart.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.AutoStartStatus)]?.ToString() ?? "false");

		ForwardRewindButtonVisibility.IsOn = bool.Parse(localSettings.Values[nameof(LocalSave.ForwardRewindButtonVisibility)]?.ToString() ?? "true");
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

	/// <summary>
	/// Handles the event triggered when the toggle switch for "Minimize to Tray" is toggled on the settings page.
	/// Updates the corresponding application setting and configures the minimize behavior of the main application window.
	/// </summary>
	/// <param name="sender">The source of the event, typically the toggle switch control named "MinimizeToTray".</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private void MinimizeToTray_OnToggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.MinimizeToTray)] = MinimizeToTray.IsOn;
		MainWindow.SetMinimizeBehaviourStatic(MinimizeToTray.IsOn);
	}

	/// <summary>
	/// Handles the toggled event for the "Artists" toggle switch in the settings page.
	/// Updates the visibility of the "Artists" navigation library item in the application's menu,
	/// modifies associated application settings, and adjusts the behavior of the music player
	/// if the "Artists" view is currently active.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Artists" toggle switch.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private async void ArtistsToggle_Toggled(object sender, RoutedEventArgs e)
	{
		var librariesGroup = App.Current.NavService.MenuItems[1] as NavigationViewItem;
		var libraryNavigationItem = librariesGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.ArtistsViewPage");

		if (libraryNavigationItem != null) libraryNavigationItem.Visibility = ArtistsToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

		//TODO: Ask confirmation when currently playing from it

		if (!ArtistsToggle.IsOn)
		{
			MainPage._instance.RemovePageFromHistory("Artists");
			MainPage._instance.RemovePageFromHistory("Tunetastic.Views.LibraryViews.ArtistDetailPage");
		}

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.ArtistsEnabled)] = ArtistsToggle.IsOn;

		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString()?.StartsWith("ArtistGroup>") == true)
		{
			localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "AllSongsViewPage";
			MusicPlayer.Instance.ResetOrReloadPlayer();
		}
	}

	/// <summary>
	/// Handles the toggled event for the "Albums" toggle switch.
	/// This method is responsible for updating the visibility of the Albums library section in the UI,
	/// storing the toggle state in application settings, and updating the music player state
	/// if the Albums playlist is currently selected.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Albums" toggle switch.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private async void AlbumsToggle_Toggled(object sender, RoutedEventArgs e)
	{
		var librariesGroup = App.Current.NavService.MenuItems[1] as NavigationViewItem;
		var libraryNavigationItem = librariesGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.AlbumsViewPage");

		if (libraryNavigationItem != null) libraryNavigationItem.Visibility = AlbumsToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

		//TODO: Ask confirmation when currently playing from it

		if (!AlbumsToggle.IsOn)
		{
			MainPage._instance.RemovePageFromHistory("Albums");
			MainPage._instance.RemovePageFromHistory("Tunetastic.Views.LibraryViews.AlbumDetailPage");
		}

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.AlbumsEnabled)] = AlbumsToggle.IsOn;

		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "AlbumGroup>")
		{
			localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "AllSongsViewPage";
			MusicPlayer.Instance.ResetOrReloadPlayer();
		}
	}

	/// <summary>
	/// Handles the toggled state change of the "Genres" toggle switch.
	/// This method updates the visibility of the "Genres" section in the library navigation,
	/// manages the application's internal state for the "Genres" feature, and resets
	/// the music player if the current playlist is set to "Genres".
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Genres" toggle switch.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private async void GenresToggle_Toggled(object sender, RoutedEventArgs e)
	{
		var librariesGroup = App.Current.NavService.MenuItems[1] as NavigationViewItem;
		var libraryNavigationItem = librariesGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.GenresViewPage");

		if (libraryNavigationItem != null) libraryNavigationItem.Visibility = GenresToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

		//TODO: Ask confirmation when currently playing from it

		if (!GenresToggle.IsOn)
		{
			MainPage._instance.RemovePageFromHistory("Genres");
			MainPage._instance.RemovePageFromHistory("Tunetastic.Views.LibraryViews.GenreDetailPage");
		}

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.GenresEnabled)] = GenresToggle.IsOn;

		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "GenreGroup>")
		{
			localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "AllSongsViewPage";
			MusicPlayer.Instance.ResetOrReloadPlayer();
		}
	}

	/// <summary>
	/// Handles the toggled event of the "Years" toggle switch in the settings page.
	/// This method updates the visibility of the Years library navigation item,
	/// adjusts the application settings, and resets the music player if necessary.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Years" toggle switch.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private async void YearsToggle_Toggled(object sender, RoutedEventArgs e)
	{
		var librariesGroup = App.Current.NavService.MenuItems[1] as NavigationViewItem;
		var libraryNavigationItem = librariesGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.LibraryViews.YearsViewPage");

		if (libraryNavigationItem != null) libraryNavigationItem.Visibility = YearsToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

		//TODO: Ask confirmation when currently playing from it

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		if (!YearsToggle.IsOn)
		{
			MainPage._instance.RemovePageFromHistory("Years");
			MainPage._instance.RemovePageFromHistory("Tunetastic.Views.LibraryViews.YearDetailPage");
		}

		localSettings.Values[nameof(LocalSave.YearsEnabled)] = YearsToggle.IsOn;

		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "YearGroup>")
		{
			localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "AllSongsViewPage";
			MusicPlayer.Instance.ResetOrReloadPlayer();
		}
	}

	/// <summary>
	/// Handles the toggled state change of the "Recently Added" toggle switch.
	/// This method is responsible for updating the visibility of the "Recently Added" playlist UI element,
	/// saving the toggle state to local settings, and performing necessary updates to the music player
	/// based on the enabled or disabled state of the "Recently Added" feature.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Recently Added" toggle switch.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private async void RecentlyAddedToggle_Toggled(object sender, RoutedEventArgs e)
	{
		var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
		var playListNavigationItem = playlistsGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.PlaylistViews.RecentlyAdded");

		if (playListNavigationItem != null) playListNavigationItem.Visibility = RecentlyAddedToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

		//TODO: Ask confirmation when currently playing from it

		if (!RecentlyAddedToggle.IsOn) MainPage._instance.RemovePageFromHistory("Recently Added");

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.RecentlyAddedEnabled)] = RecentlyAddedToggle.IsOn;

		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "RecentlyAdded")
		{
			localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "AllSongsViewPage";
			MusicPlayer.Instance.ResetOrReloadPlayer();
		}
	}

	/// <summary>
	/// Handles the toggling event of the "Recently Played" toggle switch.
	/// This method enables or disables the visibility of the "Recently Played" playlist in the user interface
	/// and updates the application state accordingly.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Recently Played" toggle switch.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private async void RecentlyPlayedToggle_Toggled(object sender, RoutedEventArgs e)
	{
		var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
		var playListNavigationItem = playlistsGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.PlaylistViews.RecentlyPlayed");

		if (playListNavigationItem != null) playListNavigationItem.Visibility = RecentlyPlayedToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

		//TODO: Ask confirmation when currently playing from it

		if (!RecentlyPlayedToggle.IsOn) MainPage._instance.RemovePageFromHistory("Recently Played");

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.RecentlyPlayedEnabled)] = RecentlyPlayedToggle.IsOn;

		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "RecentlyPlayed")
		{
			localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "AllSongsViewPage";
			MusicPlayer.Instance.ResetOrReloadPlayer();
		}
	}

	/// <summary>
	/// Handles the toggled event of the "Most Played" toggle switch.
	/// This method updates the visibility of the "Most Played" playlist in the application's navigation menu,
	/// manages local application settings related to the feature, and adjusts the media player accordingly.
	/// </summary>
	/// <param name="sender">The source of the event, typically the "Most Played" toggle switch.</param>
	/// <param name="e">An instance of RoutedEventArgs containing the event data.</param>
	private async void MostPlayedToggle_Toggled(object sender, RoutedEventArgs e)
	{
		var playlistsGroup = App.Current.NavService.MenuItems[2] as NavigationViewItem;
		var playListNavigationItem = playlistsGroup?.MenuItems.Select(x => x as NavigationViewItem).FirstOrDefault(x => x?.Tag.ToString() == "Tunetastic.Views.PlaylistViews.MostPlayed");

		if (playListNavigationItem != null) playListNavigationItem.Visibility = MostPlayedToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

		//TODO: Ask confirmation when currently playing from it

		if (!MostPlayedToggle.IsOn) MainPage._instance.RemovePageFromHistory("Most Played");

		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		localSettings.Values[nameof(LocalSave.MostPlayedEnabled)] = MostPlayedToggle.IsOn;

		if (localSettings.Values[nameof(LocalSave.CurrentPlayinglist)]?.ToString() == "MostPlayed")
		{
			localSettings.Values[nameof(LocalSave.CurrentPlayinglist)] = "AllSongsViewPage";
			MusicPlayer.Instance.ResetOrReloadPlayer();
		}
	}

	private void Page_ActualThemeChanged(FrameworkElement? sender, object? args)
	{
		SourceCodeImage.Source = new BitmapImage(new Uri(App.Current.ThemeService.IsDark ? "ms-appx:///Assets/Store/GitHub_Invertocat_White.png" : "ms-appx:///Assets/Store/GitHub_Invertocat_Black.png"));
		MicrosoftStoreImage.Source = new BitmapImage(new Uri(App.Current.ThemeService.IsDark ? "ms-appx:///Assets/Store/MS_Dark.png" : "ms-appx:///Assets/Store/MS_Light.png"));
	}

	private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
	{
		CheckForUpdates.ProgressRingVisibility = Visibility.Visible;
		var context = StoreContext.GetDefault();

		WinRT.Interop.InitializeWithWindow.Initialize(context,
			WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow));

		var updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();

		if (updates.Count == 0)
		{
			CheckForUpdates.ProgressRingVisibility = Visibility.Collapsed;
			CheckForUpdates.IsChecked = false;
			await MessageBox.ShowSuccessAsync(isModal: true, owner: App.MainWindow, "Your app is up to date.", "Update check", buttons: MessageBoxButtons.OK);
		}
		else
		{
			var result = await context.RequestDownloadAndInstallStorePackageUpdatesAsync(updates).AsTask();

			if (result.OverallState == StorePackageUpdateState.Completed)
			{
				CheckForUpdates.ProgressRingVisibility = Visibility.Collapsed;
				await MessageBox.ShowInfoAsync(isModal: true, owner: App.MainWindow, "Update installed", "The update will apply next time you launch the app.");
			}
			else
			{
				CheckForUpdates.ProgressRingVisibility = Visibility.Collapsed;
				CheckForUpdates.IsChecked = false;
			}
		}
	}

	private void LoadAboutSectionSettings()
	{
		CheckForUpdatesStartupToggle.IsOn = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CheckForUpdatesAtStatup)]?.ToString() ?? "true");
		VersionInfoToggle.IsOn = bool.Parse(Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ShowVersionInfoOnTitleBar)]?.ToString() ?? "true");
	}

	private void CheckForUpdatesStartupToggle_Toggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.CheckForUpdatesAtStatup)] = CheckForUpdatesStartupToggle.IsOn;
	}

	private void VersionInfoToggle_Toggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ShowVersionInfoOnTitleBar)] = VersionInfoToggle.IsOn;
		MainPage._instance?.SetVersionInfoVisibility(VersionInfoToggle.IsOn);
	}

	private void ForwardRewindButtonVisibility_Toggled(object sender, RoutedEventArgs e)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.ForwardRewindButtonVisibility)] = ForwardRewindButtonVisibility.IsOn;
		App.GetService<MusicControlViewModel>().Forward_Rewind_Visibility = ForwardRewindButtonVisibility.IsOn ? Visibility.Visible : Visibility.Collapsed;
	}

	private async void RateThisAppButton_Click(object sender, RoutedEventArgs e)
	{
		MainPage._instance?.StoreRating(userInvoked: true);
	}

}
