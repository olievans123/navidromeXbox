# 🎵 Navidrome for Xbox

A beautiful, **native Xbox** music client built on **UWP + WinUI**, for
[Navidrome](https://www.navidrome.org/) and any **Subsonic / OpenSubsonic**-compatible
server. Browse your whole library, search, and stream — with **gapless playback**,
**Xbox media transport controls**, and a 10‑foot, gamepad‑first interface.

> Built to be opened in **Visual Studio on Windows** and deployed to an **Xbox in
> Developer Mode**. It also runs as a normal UWP app on a Windows PC, which is the
> fastest way to iterate. You can build it **entirely from macOS/Linux** via the
> bundled GitHub Actions workflow — see [BUILD.md](BUILD.md).

---

## ✨ Features

| | |
|---|---|
| **Connect to any server** | Sign in to Navidrome or any Subsonic/OpenSubsonic server with **salted-token auth** — your password is kept in the Windows credential vault and never sent over the wire. |
| **Home** | Recently added, recently played, most played, random picks, and your starred albums — as horizontal cover shelves. |
| **Albums** | Browse the whole library with live sorting (recently added / played, most played, by name, by artist, starred, random) and paging. |
| **Artists** | A‑Z indexed artist browser with round artist art, per‑artist albums, **biography** (`getArtistInfo2`), and **popular tracks** (`getTopSongs`). |
| **Album & playlist detail** | Cover hero, full track list, **Play / Shuffle / Add‑to‑queue**, and one‑tap **star**. |
| **Playlists** | All your playlists, with cover, metadata, and play/shuffle. |
| **Genres** | Tap a genre to start an instant shuffled mix. |
| **Radio** | Your Navidrome internet-radio stations, one click to start the live stream. |
| **Search** | Debounced, unified `search3` across **artists, albums, and songs** with rich results. |
| **Now Playing** | Full‑screen player: large art, **seekable** progress, shuffle / prev / play‑pause / next / repeat, star, and a jump to the queue. |
| **Queue** | See what's next, **jump to any track**, and clear the queue. |
| **Gapless playback** | A single `MediaPlaybackList` engine streams the queue gaplessly and wires the **System Media Transport Controls**, so the Xbox guide, the remote, and voice can play / pause / skip — with cover art and track info on the system overlay. |
| **Background playback** | Declares `backgroundMediaPlayback`, so the music keeps playing while you browse the dashboard or jump into a game. |
| **Scrobbling** | Plays are reported back to Navidrome (and any connected Last.fm / ListenBrainz), with proper "now playing" + completion thresholds. Toggle in Settings. |
| **Transcoding** | Optional max‑bitrate cap and target format (MP3 / Opus / AAC) for slower links — the server transcodes on the fly. |
| **Customizable** | Settings let you choose which Home shelves and which side‑menu sections appear, so the app shows only what you use. |

The whole UI is tuned for a TV: large type, controller focus animations, and a
"midnight studio" violet + cyan palette.

---

## 🧱 Architecture

```
NavidromeXbox/
├─ App.xaml(.cs)              App bootstrap, dark theme, crash surface, queue save on suspend
├─ MainPage.xaml(.cs)         SplitView shell + the persistent mini player
├─ Styles/Theme.xaml          Palette, type ramp, focusable styles, shared item templates
├─ Controls/
│  └─ NowPlayingBar           Docked mini player (binds to the shared PlaybackService)
├─ Services/
│  ├─ Settings.cs             Persisted server / creds (vault) + playback prefs
│  ├─ SubsonicAuth.cs         Salted-token query builder + connection test (ping)
│  ├─ SubsonicApiService.cs   REST/JSON wrapper → DTOs; cover-art & stream URL builders
│  ├─ PlaybackService.cs      MediaPlaybackList engine, SMTC, queue, shuffle/repeat, scrobble
│  └─ AppState.cs             Process-wide api + player + user singleton
├─ Models/SubsonicModels.cs   DTOs (album, artist, song, playlist, genre, search)
├─ Helpers/                   MD5/salt, formatting, value converters, gamepad focus
├─ ViewModels/                Tiny ObservableObject + RelayCommand
└─ Views/                     Login, Home, Albums, AlbumDetail, Artists, ArtistDetail,
                              Playlists, PlaylistDetail, Genres, Search, NowPlaying,
                              Queue, Settings
```

**No third‑party music SDK** — playback is the platform `MediaPlayer` + `MediaPlaybackList`,
which gives gapless streaming and the system transport controls for free. The only NuGet
dependencies are `Newtonsoft.Json` and the UWP platform package.

---

## 🔑 How sign-in works

Subsonic uses **salted-token authentication**: every request carries
`u=<user>&t=md5(password+salt)&s=<salt>` with a fresh random salt, so the password is
never transmitted. The app stores the password in the Windows **PasswordVault** purely to
sign each request, and the server URL / username in local settings — so you stay signed in
between sessions. **Sign out** clears all of it.

Point it at your server's base URL (e.g. `https://music.example.com`) — the `/rest` path is
added automatically.

---

## 🛠 Prerequisites

- **Windows 10 (1809+) or Windows 11**
- **Visual Studio 2022** with the **Universal Windows Platform development** workload
- **Windows 10/11 SDK 10.0.22621.0** (or adjust `TargetPlatformVersion` in the `.csproj`)

## ▶️ Build & run on a PC (fastest loop)

1. Open `NavidromeXbox.sln` in Visual Studio.
2. If prompted about a signing certificate: open `Package.appxmanifest` →
   **Packaging** → **Choose Certificate…** → **Create…** (VS usually does this on first build).
3. Set the configuration to **x64 / Debug** and target **Local Machine**.
4. Press **F5**, then sign in on the first screen.

> A wired or virtual gamepad works on the PC too; keyboard arrows + Enter also drive the
> UI, so you can validate the controller UX without a console.

## 🎮 Deploy to an Xbox (Developer Mode)

1. On the Xbox, install **Dev Mode Activation** from the Store and activate Developer Mode.
2. **Dev Home → Remote Access**: note the console IP, set a pairing username/password.
3. In Visual Studio, set the configuration to **x64** and the target to **Remote Machine**.
4. In **Project → Properties → Debug**, enter the Xbox IP and choose *Universal (Unencrypted
   Protocol)*; pair when prompted.
5. Press **F5** to deploy and launch.

The app declares the `Windows.Xbox` device family, draws into the TV title‑safe area, and is
entirely controller‑navigable.

*No Windows machine? Build the sideload package on a free GitHub Windows runner and upload it
to the console from your browser — see [BUILD.md](BUILD.md).*

---

## 🎯 Controls

| Action | Gamepad | Keyboard |
|---|---|---|
| Move focus | D‑pad / Left stick | Arrow keys / Tab |
| Activate | **A** | Enter |
| Back | **B** | Esc |
| Navigation drawer | **View** (⧉), or the on‑screen ☰ | — |
| Options on the focused item | **Menu** (☰) — play, play next, add to queue, go to album/artist, star | — |
| Now Playing | **Y** | — |
| Play / pause | **X**, the transport buttons, or the **Xbox guide / remote** | — |
| Previous / next track | **LB / RB**, or the **Xbox guide / remote** | — |
| Seek | Focus the progress bar, **A** to engage, then **left/right** | Arrow keys |

Focus always lands on real content when a page opens, so the stick is never dead. Every list
and grid scrolls and virtualizes itself, and the **Menu** button opens a context menu on
whatever is focused — songs, albums, artists, and playlists each get the relevant actions.

---

## 🖼 Replacing the placeholder art

`Assets/*.png` are solid‑colour placeholders so the project builds immediately. Drop in real
tile/splash/logo art at the same filenames (see `Package.appxmanifest`). Visual Studio's
**Asset Generator** (double‑click the manifest → *Visual Assets*) produces every scaled
variant from a single source image.

---

## 🗺 Roadmap

- [x] **M0 — Foundation**: native shell, Subsonic auth, gapless player + SMTC, mini player.
- [x] **M1 — Browse**: Home shelves, Albums (sortable + paged), Artists (indexed), detail pages.
- [x] **M2 — Discover**: Genres mixes, unified search, starred.
- [x] **M3 — Playback**: full Now Playing, seek, shuffle/repeat, queue management, scrobbling.
- [x] **M4 — Account**: transcoding prefs, server info, secure sign-out.
- [x] **Discover, pt. 2**: internet radio, customizable Home shelves & side menu, background playback.
- [x] **Controller**: full gamepad scheme (View/Menu/Y/X/LB/RB), per-item context menus
      (play / play next / add to queue / go to album·artist / star), focus-first navigation.
- [ ] **Long tail**: playlist editing on-device, add-to-playlist, inline live star refresh,
      offline cache, podcasts, resume-queue on launch, lyrics (`getLyrics`), ARM64 build.

---

Made with ☕ and 🎧. Navidrome is free/libre software — consider
[supporting it](https://www.navidrome.org/).
