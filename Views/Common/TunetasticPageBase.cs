using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Shapes;

namespace Tunetastic.Views.Common;

/// <summary>
/// Common root for library and playlist pages. Owns the scanning-aware initialization flow,
/// the empty-library message rotation and shared named-control resolution.
/// </summary>
public abstract partial class TunetasticPageBase : Page
{
	/// <summary>
	/// Dispatcher queue of the UI thread this page was created on.
	/// </summary>
	protected readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

	private StackPanel? _goToSettingsPrompt;
	private StackPanel? _loadingProgressIndicator;
	private Rectangle? _progressFillRectangle;
	private TextBlock? _progressFillLabel;
	private StackPanel? _pageButtonsPanel;
	private ToggleButton? _multiSelectToggle;
	private Grid? _contentAreaGrid;
	private StackPanel? _alphabetPanel;

	private T RequiredControl<T>(string name) where T : FrameworkElement =>
		(T)(FindName(name) ?? throw new InvalidOperationException($"Control '{name}' was not found on {GetType().Name}."));

	/// <summary>The "go to settings" prompt shown while the library is empty or scanning.</summary>
	protected StackPanel GoToSettingsPrompt => _goToSettingsPrompt ??= RequiredControl<StackPanel>("GoToSettings");

	/// <summary>The container hosting the scan progress animation.</summary>
	protected StackPanel LoadingProgressIndicator => _loadingProgressIndicator ??= RequiredControl<StackPanel>("LoadingProgress");

	/// <summary>The fill bar visualizing scan progress.</summary>
	protected Rectangle ProgressFillRectangle => _progressFillRectangle ??= RequiredControl<Rectangle>("ProgressFill");

	/// <summary>The percentage label of the scan progress animation.</summary>
	protected TextBlock ProgressFillLabel => _progressFillLabel ??= RequiredControl<TextBlock>("ProgressFillText");

	/// <summary>The action bar shown once the page has content to work with.</summary>
	protected StackPanel PageButtonsPanel => _pageButtonsPanel ??= RequiredControl<StackPanel>("PageButtons");

	/// <summary>The toggle switching the song/tile views between single and multi selection.</summary>
	protected ToggleButton MultiSelectToggle => _multiSelectToggle ??= RequiredControl<ToggleButton>("MultiSelectButton");

	/// <summary>The root content area used for layout measurements.</summary>
	protected Grid ContentAreaGrid => _contentAreaGrid ??= RequiredControl<Grid>("ContentGrid");

	/// <summary>The vertical A-Z quick navigation panel. Only present on pages that show it.</summary>
	protected StackPanel AlphabetPanel => _alphabetPanel ??= RequiredControl<StackPanel>("AlphabetNavigationPanel");

	/// <summary>
	/// Runs the shared initialization flow: shows the settings prompt, lets the page collapse its content views,
	/// plays the scan progress animation while a library scan is running (recreating the page afterwards),
	/// otherwise hands over to <see cref="OnLibraryReadyAsync"/> once songs exist.
	/// </summary>
	/// <returns>A task representing the asynchronous initialization.</returns>
	protected async Task RunScanningAwareInit()
	{
		GoToSettingsPrompt.Visibility = Visibility.Visible;
		OnInitializingContent();
		PageButtonsPanel.Visibility = Visibility.Collapsed;

		if (LibraryScanner.IsScanning)
		{
			GoToSettingsPrompt.Visibility = Visibility.Collapsed;
			LoadingProgressIndicator.Opacity = 0;
			LoadingProgressIndicator.Visibility = Visibility.Visible;

			for (double i = 0; i <= 1; i += 0.05)
			{
				LoadingProgressIndicator.Opacity = i;
				await Task.Delay(1);
			}

			while (LibraryScanner.IsScanning)
			{
				ProgressFillRectangle.Width = LibraryScanner.ScanProgress * 4;
				ProgressFillLabel.Text = $"{LibraryScanner.ScanProgress.ToString()}%";
				await Task.Delay(1);
			}

			for (double i = 1; i >= 0; i -= 0.05)
			{
				LoadingProgressIndicator.Opacity = i;
				await Task.Delay(1);
			}
			LoadingProgressIndicator.Visibility = Visibility.Collapsed;
			await _dispatcherQueue.EnqueueAsync(() =>
			{
				this.Content = CreateFreshPage();
			});
			return;
		}

		if (await DatabaseHelper.Instance.GetSongsCount() > 0)
		{
			GoToSettingsPrompt.Visibility = Visibility.Collapsed;
			await OnLibraryReadyAsync();
		}
	}

	/// <summary>Collapses this page's content views and performs other pre-initialization setup.</summary>
	protected abstract void OnInitializingContent();

	/// <summary>Creates a fresh instance of this page, used to reload content after a scan completes.</summary>
	protected abstract Page CreateFreshPage();

	/// <summary>Shows page controls and loads data after the library is confirmed non-empty.</summary>
	/// <returns>A task representing the asynchronous data load.</returns>
	protected abstract Task OnLibraryReadyAsync();

	/// <summary>Displays a random empty-library quip from <see cref="EmptyLibraryMessages"/> next to the settings prompt.</summary>
	protected void AddGoToSettingsMessage()
	{
		var messages = EmptyLibraryMessages;
		if (messages.Count == 0) return;

		string message = messages[Random.Shared.Next(messages.Count)];
		var lines = message.Split('\n');

		var textBlock = RequiredControl<TextBlock>("GoToSettingsTextBlock");
		textBlock.Inlines.Clear();
		textBlock.Inlines.Add(new Run
		{
			Text = lines[0],
			FontStyle = Windows.UI.Text.FontStyle.Italic
		});

		textBlock.Inlines.Add(new LineBreak());

		textBlock.Inlines.Add(new Run
		{
			Text = lines[1]
		});
	}

	/// <summary>The witty empty-library messages shown by <see cref="AddGoToSettingsMessage"/>. Empty by default.</summary>
	protected virtual List<string> EmptyLibraryMessages { get; } = new();
}
