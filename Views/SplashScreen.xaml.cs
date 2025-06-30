using Microsoft.UI.Xaml.Media.Imaging;

namespace Tunetastic.Views;

/// <summary>
/// Represents the splash screen page of the application.
/// </summary>
/// <remarks>
/// The <c>SplashScreen</c> class serves as the initial screen displayed when the application launches.
/// It showcases a splash image and initializes background checks or processes required at startup.
/// </remarks>
public sealed partial class SplashScreen : Page
{
	public SplashScreen()
	{
		this.InitializeComponent();
		var theme = this.ActualTheme;
		SplashImage.Source = new BitmapImage(new Uri(theme == ElementTheme.Dark ? "ms-appx:///Assets/Store/Splash_Dark.png" : "ms-appx:///Assets/Store/Splash_Light.png"));
		_ = CheckScanning();
	}

	/// <summary>
	/// Monitors and manages the status of a background music scanning process.
	/// </summary>
	/// <returns>
	/// A task that represents the asynchronous operation.
	/// The task completes when either the scanning status changes or a timeout occurs.
	/// Progress updates and visibility changes for the custom progress bar are managed during the scanning process.
	/// </returns>
	private async Task CheckScanning()
	{
		int time = 0;
		while (!GetMusicData.IsScanning)
		{
			time += 100;
			await Task.Delay(100);
			if (time > 3000) break;
		}
		if (GetMusicData.IsScanning)
		{
			CustomProgressBar.Visibility = Visibility.Visible;
			for (double i = 0; i <= 1; i += 0.05)
			{
				CustomProgressBar.Opacity = i;
				await Task.Delay(1);
			}

			while (GetMusicData.IsScanning)
			{
				ProgressFill.Width = GetMusicData.ScanProgress * 4;
				ProgressFillText.Text = $"{GetMusicData.ScanProgress.ToString()}%";
				await Task.Delay(1);
			}

			for (double i = 1; i >= 0; i -= 0.05)
			{
				CustomProgressBar.Opacity = i;
				await Task.Delay(1);
			}
			CustomProgressBar.Visibility = Visibility.Collapsed;
		}
	}
}
