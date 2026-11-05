using System;
using System.Collections.Generic;
using System.Linq;
using Tunetastic.Common;

namespace Tunetastic.Overlay;

/// <summary>
/// Static registry of all layout display names and descriptions.
/// Bind OverlayLayoutCatalog.All to your dropdown ItemsSource.
/// </summary>
public class OverlayLayoutCatalog
{
	public static IReadOnlyList<OverlayLayoutInfo> All { get; } = new List<OverlayLayoutInfo>
	{
		new() { Layout = OverlayLayout.CompactPill },
		new() { Layout = OverlayLayout.CenteredPill },
		new() { Layout = OverlayLayout.RightDock },
		new() { Layout = OverlayLayout.FullArtBar },
		new() { Layout = OverlayLayout.HoverReveal },
		new() { Layout = OverlayLayout.IconStrip },
		new() { Layout = OverlayLayout.TextOnly },
		new() { Layout = OverlayLayout.TextOnlyReversed },
		new() { Layout = OverlayLayout.MarqueeTicker },
		new() { Layout = OverlayLayout.ArcRing },
		new() { Layout = OverlayLayout.TopAccentStripe },
		new() { Layout = OverlayLayout.BottomAccentStripe },
		new() { Layout = OverlayLayout.TopAlbumAccentStripe },
		new() { Layout = OverlayLayout.AlbumTint },
		new() { Layout = OverlayLayout.AlbumTintProgress },
		new() { Layout = OverlayLayout.AccentAncientScroll },
		new() { Layout = OverlayLayout.QueuePreview },
	};

	/// <summary>
	/// Resolves a setting string (which may be an enum name, legacy English display name, or localized name)
	/// to an OverlayLayout value, falling back safely to CompactPill.
	/// </summary>
	public static OverlayLayout FromSettingString(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return OverlayLayout.CompactPill;
		}

		// 1. Try parsing directly as enum name (language-neutral format: "CompactPill")
		if (Enum.TryParse<OverlayLayout>(value, ignoreCase: true, out var layout))
		{
			return layout;
		}

		// 2. Check legacy English display names (saved before localization)
		var legacyLayout = value switch
		{
			"Compact Pill" => OverlayLayout.CompactPill,
			"Centered Pill" => OverlayLayout.CenteredPill,
			"Right Dock" => OverlayLayout.RightDock,
			"Full Art Bar" => OverlayLayout.FullArtBar,
			"Hover Reveal" => OverlayLayout.HoverReveal,
			"Icon Strip" => OverlayLayout.IconStrip,
			"Text Only" => OverlayLayout.TextOnly,
			"Text Only Reversed" => OverlayLayout.TextOnlyReversed,
			"Marquee Ticker" => OverlayLayout.MarqueeTicker,
			"Arc Ring" => OverlayLayout.ArcRing,
			"Top Accent Stripe" => OverlayLayout.TopAccentStripe,
			"Bottom Accent Stripe" => OverlayLayout.BottomAccentStripe,
			"Top Album Accent Stripe" => OverlayLayout.TopAlbumAccentStripe,
			"Album Tint" => OverlayLayout.AlbumTint,
			"Album Tint Progress" => OverlayLayout.AlbumTintProgress,
			"Accent Ancient Scroll" => OverlayLayout.AccentAncientScroll,
			"Queue Preview" => OverlayLayout.QueuePreview,
			_ => (OverlayLayout?)null
		};

		if (legacyLayout.HasValue)
		{
			return legacyLayout.Value;
		}

		// 3. Check current localized DisplayNames (in case a localized string was saved)
		var match = All.FirstOrDefault(item => string.Equals(item.DisplayName, value, StringComparison.OrdinalIgnoreCase));
		if (match is not null)
		{
			return match.Layout;
		}

		// Fallback default
		return OverlayLayout.CompactPill;
	}
}
