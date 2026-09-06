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
			BorderBrush = new SolidColorBrush(color ?? Colors.DodgerBlue),
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

		var point = target.TransformToVisual(target.XamlRoot.Content).TransformPoint(new Point(0, 0));
		popup.HorizontalOffset = point.X - 6;
		popup.VerticalOffset = point.Y - 6;
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
		storyboard.Completed += (_, _) => popup.IsOpen = false;
		storyboard.Begin();
	}

	public static void HighlightAndBringIntoView(this FrameworkElement target, Color? color = null, int pulses = 2, int pulseDurationMs = 500)
	{
		target.StartBringIntoView();
		target.DispatcherQueue.TryEnqueue(async () =>
		{
			await Task.Delay(150);
			target.Highlight(color, pulses, pulseDurationMs);
		});
	}
}
