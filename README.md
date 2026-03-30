<div align="center">

<img src="Assets/AppIcon.png" alt="Tunetastic Logo" width="120" height="120" />

# Tunetastic

**A next-gen music player for Windows 10 & 11**

*Sleek design meets deep personalization — your library, your experience.*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?logo=windows11)](https://microsoft.com/windows)
[![Framework](https://img.shields.io/badge/Framework-WinUI%203-512BD4?logo=dotnet)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-GPL%20v3-blue)](LICENSE)

[<img src="https://get.microsoft.com/images/en-us%20dark.svg" alt="Get it from Microsoft Store" height="52" />](#)

*🆓 30-day free trial — then a one-time purchase, no subscription.*

</div>

---

## Overview

Tunetastic is a modern music player built natively for Windows using WinUI 3. It brings Windows 11's design language — Mica, Acrylic, rounded corners, and fluid animations — to both Windows 10 and Windows 11, combining a beautifully crafted interface with deep customization options. Turn your local music library into an experience that's as unique, expressive, and captivating as the music you love.

> 📸 *Screenshot: Main player page with blur background and playback controls*

---

## Features

### 🎵 Player

The main player page is the heart of Tunetastic — a full-screen immersive experience built around your currently playing track.

- Dynamic blurred album art background (blur intensity adjustable)
- Standard playback controls: play/pause, previous, next, seek forward/rewind
- Shuffle and repeat modes
- Track progress bar with seek support

---

### 📚 Library

Tunetastic automatically organizes your music collection into clean, browsable views:

| View | Description |
|---|---|
| All Songs | Every track in your library |
| Artists | Grouped by artist name |
| Albums | Grouped by album, with artwork |
| Genres | Sorted by genre tag |
| Years | Sorted by release year |

> 📸 *Screenshot: Library tabs showing Artists and Albums view*

---

### 📋 Playlists

Smart playlists keep your most-reached-for music always at hand, and you can build your own on top of them.

**Built-in smart playlists:**
- **Recently Added** — Newest arrivals in your library
- **Recently Played** — Picks up right where you left off
- **Most Played** — Your personal all-time favorites

**User playlists:**
- Create your own playlists and give them any name
- Rename or delete user-created playlists at any time
- Add individual tracks, or bulk-add entire albums, genres, or years in one go

> 📸 *Screenshot: Playlists sidebar showing smart playlists and user-created playlists*

---

### 🔍 Quick Search

A quick search bar sits in the top panel, letting you instantly search across your entire library by song title, artist, album, genre, or year — no need to navigate away from wherever you are.

---

### 🖥️ System Integration

Tunetastic integrates deeply with Windows for a native, seamless experience — all system integration features work on both Windows 10 and Windows 11.

**🎛️ System Media Transport Controls (SMTC)**
Full SMTC support means you can control playback from anywhere:
- Windows notification / media flyout
- Any SMTC-compatible third-party app
- Keyboard media keys (play, pause, next, previous)
- Bluetooth headphones and controllers

**📊 Taskbar Progress**
Track playback progress is shown live on the Tunetastic taskbar icon, so you always know where you are in a track at a glance — without switching windows.

**🔔 System Tray**
- Minimize to the system tray on close, keeping Tunetastic out of the way but always ready
- **Click** the tray icon to play or pause instantly
- **Hover** the tray icon to see the current track and artist

---

## Settings

### 📁 Library Management

- **Add Folders** — Point Tunetastic to your local music directories
- **File Extensions** — Choose which audio formats to include
- **Minimum Track Duration** — Ignore files below a set length (great for skipping short intros or sound effects)
- **Duplicate Detection** — Options to detect and handle duplicate tracks
- **Show / Hide Library Views** — Independently toggle the visibility of Artists, Albums, Genres, Years, and each individual playlist (All Songs is always shown)
  - Note: Built-in smart playlists can be hidden; user-created playlists can be renamed or deleted

### 🎚️ Playback

- Crossfade between tracks with adjustable duration
- Additional playback behavior settings

### 🎨 Appearance & Behavior

Tunetastic gives you fine-grained control over how it looks and feels.

**Theme**
Choose between Light, Dark, or follow the system setting.

**UI Material**
Pick the backdrop material used throughout the app:

| Material | Description |
|---|---|
| Mica | Subtle tinted blur sampling your desktop wallpaper |
| Mica Alt | A higher-contrast variation of Mica |
| Acrylic | Frosted-glass blur showing content behind the window |
| Acrylic Thin | A lighter, more transparent acrylic |
| Transparent | Fully transparent window background |

When **Mica** is selected, you can also dial in a custom tint color — anywhere from fully transparent to any color you choose.

**Accent Color**
Certain player UI elements pick up your Windows accent color automatically. A shortcut to the Windows accent color settings page is available directly from within Tunetastic.

**Player Background**
Control the blur intensity of the album art background on the main player page.

**Rainbow Border**
Add an animated rainbow border to the player:
- Toggle on/off
- Configure when it appears (e.g., always, or only during playback)
- Adjust the animation speed

**Window Behavior**
- **Minimize to tray on close** — Keep Tunetastic running silently in the background instead of fully quitting

---

## Requirements

- **Windows 10** (version 1903 / Build 18362 or later) or **Windows 11**
- **.NET Framework 4.7** or later
- No additional runtime installation required — get it directly from the **Microsoft Store**


---

## Installation

### Microsoft Store *(Recommended)*

Search for **Tunetastic** in the Microsoft Store, or follow the store link once available. No manual setup or certificate trust required — just install and play.

### Building from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/tunetastic.git
cd tunetastic

# Open in Visual Studio 2026
start Tunetastic.sln
```

Ensure the following workloads are installed in Visual Studio 2026:
- **.NET desktop development**
- **Windows application development** (includes Windows App SDK)

Then press **F5** to build and run.

---

## Contributing

Contributions are welcome! Please open an issue to discuss what you'd like to change before submitting a pull request. Make sure your code follows the existing style and that all builds pass cleanly.

---

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

---

<div align="center">

Made with ♥ for music lovers on Windows

</div>
