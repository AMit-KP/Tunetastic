using Microsoft.Win32;
using Windows.UI.ViewManagement;

namespace Tunetastic.Common.Services;

/// <summary>
/// Manages the system tray icon appearance and dynamic style switching.
/// Supports Full color, Dark, Light, and Monochrome (Auto) based on Windows taskbar theme.
/// </summary>
public static class TrayIconService
{
	private static readonly UISettings _uiSettings = new();
	private static bool _isListeningToThemeChanged;

	/// <summary>
	/// Initializes the tray icon with the saved user preference.
	/// </summary>
	public static void Initialize()
	{
		ApplyCurrentStyle();
	}

	/// <summary>
	/// Gets the initial icon path to use when creating the system tray icon.
	/// </summary>
	public static string GetInitialIconPath()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var style = localSettings.Values[nameof(LocalSave.SystemTrayIconStyle)]?.ToString() ?? "MonochromeAuto";
		return ResolveIconPath(style);
	}

	/// <summary>
	/// Applies the currently persisted tray icon style from local settings.
	/// </summary>
	public static void ApplyCurrentStyle()
	{
		var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		var style = localSettings.Values[nameof(LocalSave.SystemTrayIconStyle)]?.ToString() ?? "MonochromeAuto";
		ApplyStyle(style);
	}

	/// <summary>
	/// Saves and applies the specified tray icon style.
	/// </summary>
	/// <param name="style">The style tag (FullColor, MonochromeAuto, Dark, Light).</param>
	public static void SetStyle(string style)
	{
		Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(LocalSave.SystemTrayIconStyle)] = style;
		ApplyStyle(style);
	}

	/// <summary>
	/// Applies the requested tray icon style, updating theme listeners as appropriate.
	/// </summary>
	private static void ApplyStyle(string style)
	{
		if (style == "MonochromeAuto")
		{
			if (!_isListeningToThemeChanged)
			{
				_uiSettings.ColorValuesChanged += OnColorValuesChanged;
				_isListeningToThemeChanged = true;
			}
		}
		else if (_isListeningToThemeChanged)
		{
			_uiSettings.ColorValuesChanged -= OnColorValuesChanged;
			_isListeningToThemeChanged = false;
		}

		var iconPath = ResolveIconPath(style);
		if (App.TrayIcon is not null && !string.IsNullOrEmpty(iconPath))
		{
			try
			{
				App.TrayIcon.SetIcon(iconPath);
			}
			catch
			{
				// Fallback to default icon if an issue occurs
				var defaultIcon = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon_Full.ico");
				if (!File.Exists(defaultIcon))
				{
					defaultIcon = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
				}

				if (File.Exists(defaultIcon))
				{
					App.TrayIcon.SetIcon(defaultIcon);
				}
			}
		}
	}

	/// <summary>
	/// Resolves the absolute path to the appropriate .ico file based on style and taskbar theme.
	/// </summary>
	private static string ResolveIconPath(string style)
	{
		var fullColorIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon_Full.ico");
		var defaultIconPath = File.Exists(fullColorIconPath)
			? fullColorIconPath
			: Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
		var lightIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon_Light.ico");
		var darkIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon_Dark.ico");

		string targetPath = style switch
		{
			"Dark" => darkIconPath,
			"Light" => lightIconPath,
			"MonochromeAuto" => IsTaskbarThemeDark() ? lightIconPath : darkIconPath,
			"FullColor" => defaultIconPath,
			_ => defaultIconPath
		};

		return File.Exists(targetPath) ? targetPath : defaultIconPath;
	}

	/// <summary>
	/// Checks whether the Windows taskbar/system theme is currently dark.
	/// </summary>
	public static bool IsTaskbarThemeDark()
	{
		try
		{
			// In WinRT, Background color represents the Windows shell/system background.
			// When taskbar is dark, background is (0, 0, 0).
			var color = _uiSettings.GetColorValue(UIColorType.Background);
			return color.R == 0 && color.G == 0 && color.B == 0;
		}
		catch
		{
			return IsTaskbarThemeDarkRegistryFallback();
		}
	}

	/// <summary>
	/// Fallback method to check taskbar theme via the Windows Registry.
	/// </summary>
	private static bool IsTaskbarThemeDarkRegistryFallback()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
			if (key?.GetValue("SystemUsesLightTheme") is int val)
			{
				return val == 0;
			}
		}
		catch
		{
			// Ignore registry read errors
		}

		return true; // Modern Windows defaults to dark taskbar
	}

	/// <summary>
	/// Event handler triggered when Windows system color values change.
	/// </summary>
	private static void OnColorValuesChanged(UISettings sender, object? args)
	{
		App.MainWindow?.DispatcherQueue?.TryEnqueue(() =>
		{
			var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
			var currentStyle = localSettings.Values[nameof(LocalSave.SystemTrayIconStyle)]?.ToString() ?? "MonochromeAuto";
			if (currentStyle == "MonochromeAuto")
			{
				ApplyStyle(currentStyle);
			}
		});
	}

	/// <summary>
	/// Cleans up event listeners when the application is shutting down.
	/// </summary>
	public static void Shutdown()
	{
		if (_isListeningToThemeChanged)
		{
			_uiSettings.ColorValuesChanged -= OnColorValuesChanged;
			_isListeningToThemeChanged = false;
		}
	}
}
