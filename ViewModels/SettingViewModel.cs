using Windows.Storage;

namespace Tunetastic.ViewModels;

public partial class SettingViewModel : ObservableObject
{
	/// <summary>
	/// Initiates a reset of all application settings and restarts the application.
	/// This method clears user preferences and persistent data, then triggers an application restart
	/// to apply the changes and reinitialize the app with default settings.
	/// </summary>
	[RelayCommand]
	void Reset()
	{
		_ = ResetAllSettings();
		RestartApp();
	}

	/// <summary>
	/// Resets all application settings to their default values.
	/// This method clears both local settings and persistent application data storage.
	/// It is commonly used when a complete reset of user preferences or stored data is required.
	/// </summary>
	/// <returns>A Task that represents the asynchronous operation.</returns>
	public async Task ResetAllSettings()
	{
		ApplicationData.Current.LocalSettings.Values.Clear();
		await ApplicationData.Current.ClearAsync();
	}

	/// <summary>
	/// Restarts the application. This method leverages the AppInstance.Restart method,
	/// allowing the application to terminate and relaunch with a blank argument or state.
	/// Primarily used after resetting application settings or applying critical updates.
	/// </summary>
	private void RestartApp()
	{
		Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
	}
}
