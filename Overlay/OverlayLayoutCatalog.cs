namespace Tunetastic.Overlay;

/// <summary>
/// Static registry of all layout display names and descriptions.
/// Bind OverlayLayoutCatalog.All to your dropdown ItemsSource.
/// </summary>
public class OverlayLayoutCatalog
{
	public static IReadOnlyList<OverlayLayoutInfo> All { get; } = new List<OverlayLayoutInfo>
		{
			new() {
				Layout      = OverlayLayout.CompactPill,
				DisplayName = "Compact Pill",
				Description = "Rounded pill with art, track info and controls always visible."
			},
			new() {
				Layout      = OverlayLayout.HoverReveal,
				DisplayName = "Hover Reveal",
				Description = "Shows only track info at rest; controls fade in on hover."
			},
			new() {
				Layout      = OverlayLayout.MarqueeTicker,
				DisplayName = "Marquee Ticker",
				Description = "Scrolling track details ticker with a micro progress bar, no album art."
			},
			new() {
				Layout      = OverlayLayout.RightDock,
				DisplayName = "Right Dock",
				Description = "Right-anchored strip with controls leading and track info trailing."
			},
			new() {
				Layout      = OverlayLayout.FullArtBar,
				DisplayName = "Full Art Bar",
				Description = "Album art bleeds full taskbar height; title, artist and progress bar beside it."
			},
			new() {
				Layout      = OverlayLayout.AccentAncientScroll,
				DisplayName = "Accent Ancient Scroll",
				Description = "Ancient scroll with accent-coloured rods, controls and track info."
			},
			new() {
				Layout      = OverlayLayout.IconStrip,
				DisplayName = "Icon Strip",
				Description = "Zero text — album art thumbnail plus control icon buttons."
			},
			new() {
				Layout      = OverlayLayout.CenteredPill,
				DisplayName = "Centered Pill",
				Description = "Controls positioned at the centre, art on the left and the track info on the right."
			},
			new() {
				Layout      = OverlayLayout.TopAccentStripe,
				DisplayName = "Top Accent Stripe",
				Description = "Thin progress accent stripe runs along the top edge of the overlay."
			},
			new() {
				Layout      = OverlayLayout.BottomAccentStripe,
				DisplayName = "Bottom Accent Stripe",
				Description = "Thin progress accent stripe runs along the bottom edge of the overlay."
			},
			new() {
				Layout      = OverlayLayout.AlbumTint,
				DisplayName = "Album Tint",
				Description = "Background tint adapts to the album's colours."
			},
			new() {
				Layout      = OverlayLayout.TextOnly,
				DisplayName = "Text Only",
				Description = "Pure text: controls and track name, artists label. No art."
			},
			new() {
				Layout      = OverlayLayout.TextOnlyReversed,
				DisplayName = "Text Only Reversed",
				Description = "Pure text: track name, artists label, and controls. No art."
			},
			new() {
				Layout      = OverlayLayout.ArcRing,
				DisplayName = "Arc Ring",
				Description = "Circular disc art with an arc progress ring."
			},
			new() {
				Layout      = OverlayLayout.QueuePreview,
				DisplayName = "Queue Preview",
				Description = "Current plus two upcoming album arts in a diminishing stack with controls."
			}
		};
}
