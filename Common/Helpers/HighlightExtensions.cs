using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI;

namespace Tunetastic.Common.Helpers;

public static class HighlightExtensions
{
	public static void Highlight(this FrameworkElement target, Color? color = null, int pulses = 2, int pulseDurationMs = 500)
	{
		if (target?.XamlRoot == null) return;

		var ring = new Border
		{
			BorderBrush = new SolidColorBrush(color ?? (Color)Application.Current.Resources["SystemAccentColor"]),
			BorderThickness = new Thickness(3),
			CornerRadius = new CornerRadius(8),
			IsHitTestVisible = false,
			Opacity = 0,
			Width = target.ActualWidth + 12,
			Height = target.ActualHeight + 12
		};

		var popup = new Popup
		{
			XamlRoot = target.XamlRoot,
			Child = ring,
			IsHitTestVisible = false
		};

		// Keep the popup glued to the target every frame while it's open.
		void UpdatePosition(object? sender, object? e)
		{
			if (target.XamlRoot == null || !target.IsLoaded)
			{
				popup.IsOpen = false;
				return;
			}

			try
			{
				var point = target.TransformToVisual(target.XamlRoot.Content)
								   .TransformPoint(new Point(0, 0));
				popup.HorizontalOffset = point.X - 6;
				popup.VerticalOffset = point.Y - 6;

				// Also keep the ring's size in sync in case the target resizes.
				ring.Width = target.ActualWidth + 12;
				ring.Height = target.ActualHeight + 12;
			}
			catch
			{
				// Target likely detached from the tree mid-animation.
				popup.IsOpen = false;
			}
		}

		void StopTracking()
		{
			CompositionTarget.Rendering -= UpdatePosition;
		}

		popup.Closed += (_, _) => StopTracking();
		target.Unloaded += (_, _) => popup.IsOpen = false;

		CompositionTarget.Rendering += UpdatePosition;
		UpdatePosition(null, null); // set initial position before opening
		popup.IsOpen = true;

		var anim = new DoubleAnimationUsingKeyFrames
		{
			RepeatBehavior = new RepeatBehavior(pulses)
		};
		anim.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0 });
		anim.KeyFrames.Add(new EasingDoubleKeyFrame
		{
			KeyTime = TimeSpan.FromMilliseconds(pulseDurationMs / 2.0),
			Value = 1
		});
		anim.KeyFrames.Add(new EasingDoubleKeyFrame
		{
			KeyTime = TimeSpan.FromMilliseconds(pulseDurationMs),
			Value = 0
		});

		var storyboard = new Storyboard();
		Storyboard.SetTarget(anim, ring);
		Storyboard.SetTargetProperty(anim, "Opacity");
		storyboard.Children.Add(anim);
		storyboard.Completed += (_, _) => popup.IsOpen = false; // triggers StopTracking via Closed
		storyboard.Begin();
	}

	public static void HighlightAndBringIntoView(this FrameworkElement target, Color? color = null, int pulses = 2, int pulseDurationMs = 500)
	{
		target.StartBringIntoView();
		target.DispatcherQueue.TryEnqueue(async () =>
		{
			await Task.Delay(500);
			target.Highlight(color, pulses, pulseDurationMs);
		});
	}
}
