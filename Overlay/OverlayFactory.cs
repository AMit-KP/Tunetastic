using Tunetastic.Overlay.Layouts;

namespace Tunetastic.Overlay;

public static class OverlayFactory
{
	/// <summary>
	/// Creates and returns the overlay for the given layout and theme.
	/// Pass overlay.RootGrid to your SetOverlayContent() call.
	/// </summary>
	public static OverlayBase Create(OverlayLayout? layout, OverlayTheme theme)
	{
		return layout switch
		{
			OverlayLayout.CompactPill => new CompactPillOverlay(theme),
			OverlayLayout.HoverReveal => new HoverRevealOverlay(theme),
			OverlayLayout.WaveformEdge => new WaveformEdgeOverlay(theme),
			OverlayLayout.MarqueeTicker => new MarqueeTickerOverlay(theme),
			OverlayLayout.LeftPill => new LeftPillOverlay(theme),
			OverlayLayout.RightDock => new RightDockOverlay(theme),
			OverlayLayout.FullArtBar => new FullArtBarOverlay(theme),
			OverlayLayout.WaveformOnly => new WaveformOnlyOverlay(theme),
			OverlayLayout.AccentEdge => new AccentEdgeOverlay(theme),
			OverlayLayout.IconStrip => new IconStripOverlay(theme),
			OverlayLayout.StackedInfo => new StackedInfoOverlay(theme),
			OverlayLayout.CenteredPill => new CenteredPillOverlay(theme),
			OverlayLayout.TopStripe => new TopStripeOverlay(theme),
			OverlayLayout.BottomStripe => new BottomStripeOverlay(theme),
			OverlayLayout.AlbumTint => new AlbumTintOverlay(theme),
			OverlayLayout.TextOnly => new TextOnlyOverlay(theme),
			OverlayLayout.ArcRing => new ArcRingOverlay(theme),
			OverlayLayout.QueuePreview => new QueuePreviewOverlay(theme),
			OverlayLayout.ArtistBadge => new ArtistBadgeOverlay(theme),

			_ => throw new ArgumentOutOfRangeException(nameof(layout),
					 $"Unknown layout: {layout}")
		};
	}
}
