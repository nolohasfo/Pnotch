# Nusa Notch: Arsitektur Awal (WPF, .NET 8, Windows 10/11)

## Lapisan
```
Pnotch/
├─ Shell/            MainWindow (borderless, topmost, no-activate), NotchStateMachine,
│                    FullscreenWatcher, IdleMonitor, MonitorPlacement
├─ Modules/          INotchModule { Id, View, OnOpened(), OnClosed() }
│   ├─ Media/        SmtcService (+ Lyrics/LrclibClient, LrcParser)
│   ├─ Focus/        PomodoroService
│   ├─ Water/        HydrationService (+ chart)
│   ├─ Shelf/        ShelfService (drag in/out)
│   ├─ Todo/         TodoService (+ reminder scheduler)
│   ├─ Clipboard/    ClipboardHistoryService      (opsional)
│   └─ Mirror/       CameraMirrorView             (opsional)
├─ Overlays/         OverlayHub: Volume, Brightness, CapsLock, KbLayout, Charger, WiFi, Headphones
└─ Storage/          JsonStore -> %LOCALAPPDATA%\Nusa\*.json (lokal saja, tanpa akun/analytics)
```
Prinsip: **semua event-driven**. Tidak ada timer polling permanen. Setiap modul men-subscribe event OS
hanya saat perlu dan menaruh UI-nya di `OpenHost` saat notch terbuka.

## State machine
`Closed ⇄ Open` (hover / drag file masuk) · `Closed → Resting` (idle 60 dtk) · `Resting → Closed` (input pertama) ·
`* → Hidden` (app lain fullscreen) · `Overlay` bersifat sementara di atas Closed (auto-dismiss ±1,5 dtk).

## Strategi 0% CPU saat idle
- Hover pakai `MouseEnter/Leave` WPF + debounce timer one-shot (tanpa polling kursor).
- Animasi WPF berhenti merender saat storyboard selesai; mata "tidur" = opacity statis.
- Idle: timer one-shot + `GetLastInputInfo`; hook input low-level hanya dipasang selama Resting.
- Fullscreen: `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`, bukan polling.
- Jam (1 dtk) hanya jalan saat Closed. Pertimbangkan menaikkan interval ke per-menit bila tidak menampilkan detik
  (selaraskan dengan batas menit via timer one-shot).
- Gelombang suara: render hanya saat `PlaybackStatus == Playing`; hentikan saat Resting/Hidden.
- Catatan jujur: "0% CPU mutlak" tidak mungkin dengan musik berjalan jika gelombang animasi tampil. Targetnya
  ~0% saat tidak ada animasi yang terlihat. Ukur dengan Windows Performance Analyzer.

## Peta fitur → API Windows
| Modul | API |
|---|---|
| Media | `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager` (Spotify, Apple Music, Chrome/Edge YouTube). Event `MediaPropertiesChanged`, `PlaybackInfoChanged`, `TimelinePropertiesChanged`. Seek: `TryChangePlaybackPositionAsync(ticks)`. Thumbnail: `MediaProperties.Thumbnail` |
| Lirik | `GET https://lrclib.net/api/get?track_name=&artist_name=&album_name=&duration=` → `syncedLyrics` (format LRC). Kirim HANYA judul/artis/durasi. Cache lokal. Highlight baris dengan timer one-shot menuju timestamp baris berikutnya (bukan polling). Klik baris → seek. Animasi "split": dua `Border` setengah lebar yang bergeser dari tengah |
| Pomodoro | `DispatcherTimer` satu detik hanya saat sesi aktif; chime via `System.Media.SoundPlayer` / `MediaPlayer` |
| Hidrasi | JSON lokal; grafik dengan `Canvas`/`Path` sederhana; reminder via timer one-shot pada jam aktif |
| File Shelf | Masuk: `Drop` event + `DataFormats.FileDrop` (simpan path/referensi, bukan salinan). Keluar: `DragDrop.DoDragDrop(src, new DataObject(DataFormats.FileDrop, paths), DragDropEffects.Copy|Move)` |
| To-do | Daftar centang (`ItemsControl`+`CheckBox`), scheduler one-shot ke tenggat terdekat, "shoulder tap" = animasi bounce kecil + expand sebentar |
| Volume | Core Audio `IAudioEndpointVolume` + `OnNotify` callback (NAudio/CSCore) |
| Brightness | WMI event `WmiMonitorBrightnessEvent` (namespace `root\wmi`) |
| Caps Lock | Hook keyboard low-level hanya mendeteksi VK_CAPITAL (callback ringan) |
| Bahasa keyboard | `EVENT_SYSTEM_FOREGROUND` + `GetKeyboardLayout(threadId)` atau `WM_INPUTLANGCHANGE` |
| Charger | `SystemEvents.PowerModeChanged` / `WM_POWERBROADCAST` + `GetSystemPowerStatus` |
| Wi-Fi | `NetworkChange.NetworkAvailabilityChanged` / `NetworkAddressChanged` |
| Headphone | `Windows.Devices.Enumeration.DeviceWatcher` (audio render) atau `IMMNotificationClient` |
| Clipboard | `AddClipboardFormatListener` → `WM_CLIPBOARDUPDATE`; simpan teks lokal; pencarian in-memory. Beri opsi hapus riwayat & kecualikan app tertentu (password manager) |
| Cermin kamera | `Windows.Media.Capture.MediaCapture` + `MediaPlayerElement`; kamera hanya aktif saat toggle ON |

## Privasi
Semua JSON di `%LOCALAPPDATA%\Nusa\`. Satu-satunya request jaringan: LRCLIB (title/artist/duration). Tidak ada akun,
telemetry, atau crash reporter otomatis.

## Catatan & risiko
- **Transparansi/click-through**: `Background=null` + `AllowsTransparency` membuat piksel transparan tidak menangkap
  klik. Jika di perangkat tertentu masih memblokir, ubah ukuran *window* ikut animasi (set `Width/Height` window pada
  `Completed`).
- **Topmost**: tegaskan ulang `SetWindowPos(HWND_TOPMOST)` saat foreground berubah (taskbar bisa menimpa).
- **Multi-monitor/DPI**: ganti `SystemParameters.PrimaryScreenWidth` dengan `Screen`/`MonitorFromPoint` + `GetDpiForMonitor`.
- **Overlay volume/brightness bawaan Windows** tetap muncul; menyembunyikannya butuh trik tidak resmi, jadi mulai dengan
  overlay Nusa berdampingan.
- **Hook low-level** dapat memicu antivirus; pertahankan callback sangat ringan dan tandatangani biner (code signing).
- Alternatif Tauri (Rust+React) cocok jika UI banyak animasi web; namun idle 0% CPU lebih sulit karena WebView2.

## Urutan pengerjaan yang disarankan
1. Shell (kerangka ini) → 2. Media + Lyrics → 3. Overlay Hub → 4. Pomodoro/Todo/Water → 5. Shelf → 6. Clipboard/Mirror → 7. Installer MSIX + autostart toggle (Registry `Run`).
