# MusicOverlay — WinUI 3 Taskbar Overlay System

All code-behind, no XAML. Drop the files into your project, call the factory, pass the grid.

---

## File Map

```
MusicOverlay/
├── OverlayDefinitions.cs     — OverlayLayout enum, OverlayTheme enum, OverlayLayoutCatalog
├── OverlayBase.cs            — Base class with shared helpers, button refs, FadeIn/Out
├── OverlayFactory.cs         — Single Create(layout, theme) entry point
└── Layouts/
    ├── Layouts_AB.cs         — CompactPill, HoverReveal
    ├── Layouts_CD.cs         — WaveformEdge, MarqueeTicker
    ├── Layouts_1234.cs       — LeftPill, RightDock, FullArtBar, WaveformOnly
    ├── Layouts_7_8_9_11.cs   — AccentEdge, IconStrip, StackedInfo, CenteredPill
    ├── Layouts_13_14_15_16.cs— TopStripe, BottomStripe, AlbumTint, TextOnly
    └── Layouts_18_19_20.cs   — ArcRing, QueuePreview, ArtistBadge
```

---

## Quick Start

```csharp
// 1. Create the overlay
var overlay = OverlayFactory.Create(OverlayLayout.CompactPill, OverlayTheme.Dark);

// 2. Pass the grid to your overlay host
SetOverlayContent(overlay.RootGrid);

// 3. Wire up buttons (null-check — not every layout has every button)
overlay.PlayPauseButton?.Click += (_, _) => player.TogglePlayPause();
overlay.PreviousButton?.Click  += (_, _) => player.Previous();
overlay.NextButton?.Click      += (_, _) => player.Next();
overlay.LikeButton?.Click      += (_, _) => player.ToggleLike();
overlay.VolumeButton?.Click    += (_, _) => OpenVolumePopup();

// 4. Update track info — cast to the concrete type for the typed UpdateTrack
if (overlay is CompactPillOverlay cp)
    cp.UpdateTrack("Neon Pulse", "Synthwave Era", albumArt);

// 5. Sync play/pause icon
overlay.SetPlayingState(player.IsPlaying);

// 6. Update progress on a timer (layouts that support it)
//    Call this from your DispatcherTimer tick
overlay.UpdateProgress(player.Position.TotalSeconds / player.Duration.TotalSeconds);
```

---

## Dropdown Binding (User Layout Picker)

```csharp
// In your settings page code-behind:

// Bind the list to a ComboBox
LayoutComboBox.ItemsSource       = OverlayLayoutCatalog.All;
LayoutComboBox.DisplayMemberPath = "DisplayName";  // shows "Compact Pill" etc.

// Or in XAML: ItemsSource="{x:Bind OverlayLayoutCatalog.All}"
//             DisplayMemberPath="DisplayName"

// On selection changed
LayoutComboBox.SelectionChanged += (s, e) =>
{
    if (LayoutComboBox.SelectedItem is OverlayLayoutInfo info)
    {
        var theme   = isDarkMode ? OverlayTheme.Dark : OverlayTheme.Light;
        var overlay = OverlayFactory.Create(info.Layout, theme);
        SetOverlayContent(overlay.RootGrid);
        // Re-wire buttons and call UpdateTrack...
    }
};
```

---

## Per-Layout UpdateTrack Signatures

Each layout only asks for the data it actually uses:

| Layout            | UpdateTrack signature |
|-------------------|-----------------------|
| CompactPill       | `(string title, string artist, BitmapImage? art)` |
| HoverReveal       | `(string title, string artist, BitmapImage? art)` |
| WaveformEdge      | `(BitmapImage? art)` |
| MarqueeTicker     | `(string title, string artist)` |
| LeftPill          | `(string title, string artist, BitmapImage? art)` |
| RightDock         | `(string title, string artist, BitmapImage? art)` |
| FullArtBar        | `(string title, string artist, BitmapImage? art)` |
| WaveformOnly      | *(no UpdateTrack — no text or art)* |
| AccentEdge        | `(string title, string artist, Color accentColor, BitmapImage? art)` |
| IconStrip         | `(BitmapImage? art)` |
| StackedInfo       | `(string title, string artist, string timestamp, BitmapImage? art)` |
| CenteredPill      | `(string title, string artist, BitmapImage? art)` |
| TopStripe         | `(string title, string artist, BitmapImage? art)` |
| BottomStripe      | `(string title, string artist, BitmapImage? art)` |
| AlbumTint         | `(string title, string artist, Color dominantColor, BitmapImage? art)` |
| TextOnly          | `(string title, string artist, string timestamp)` |
| ArcRing           | `(string title, string artist, BitmapImage? art)` |
| QueuePreview      | `(string title, string artist, BitmapImage? current, BitmapImage? next1, BitmapImage? next2)` |
| ArtistBadge       | `(string title, string artist, Color badgeColor, BitmapImage? art)` |

---

## Layouts with UpdateProgress

Call `overlay.UpdateProgress(double value)` where value is **0.0 – 1.0**:

- `FullArtBar`
- `MarqueeTicker`
- `StackedInfo`
- `TopStripe`
- `BottomStripe`
- `ArcRing`

---

## Waveform Animation Control

`WaveformEdge` and `WaveformOnly` animate decorative bars automatically.
Stop/resume them to reflect playback state:

```csharp
if (overlay is WaveformEdgeOverlay we) {
    we.StopWaveAnimation();    // call when paused
    we.ResumeWaveAnimation();  // call when playing
}
if (overlay is WaveformOnlyOverlay wo) {
    wo.StopWaveAnimation();
    wo.ResumeWaveAnimation();
}
```

---

## MarqueeTicker — DevWinUI Setup

In `Layouts_CD.cs`, look for the `// TODO` comment in `MarqueeTickerOverlay.Build()`.
Replace the plain `TextBlock` with DevWinUI's `MarqueeText`:

```csharp
// Add to your using directives:
// using DevWinUI;

var marquee = new MarqueeText
{
    Text     = $"{title} · {artist}",
    Speed    = 40,
    Behavior = MarqueeBehavior.Ticker,
};
tickerClip.Child = marquee;
// Store reference if you need to update text later
```

---

## Theme Switching

There is no auto-switch. To change theme, create a new overlay instance and
call SetOverlayContent again:

```csharp
void SwitchTheme(OverlayTheme newTheme)
{
    var overlay = OverlayFactory.Create(_currentLayout, newTheme);
    SetOverlayContent(overlay.RootGrid);
    RewireButtons(overlay);
    overlay.UpdateTrack(...);
}
```
