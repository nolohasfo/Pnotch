# 🌙 Pnotch

> A sleek, high-utility desktop notch for Windows, styled with a **Persona 3 Reload** aesthetic and dynamic **Chiikawa** vector facial expressions.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?style=flat&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green.svg)

---

## ✨ Key Features

- **🌙 Persona 3 Reload Aesthetic**: Built with Velvet Navy (`#080F20`), Sky Cyan (`#00A6FF` / `#50C8FF`), and Persona Gold (`#FFC72C`) accents.
- **🐰 Dynamic Chiikawa Faces**: Native WPF vector animations with expressions that change dynamically (Normal, Blink, Cute 🥺, and Happy).
- **⚡ Live Status Bar**: Displays real-time system battery percentage (with charging indicator) and system clock.
- **🎵 Integrated Media Player**: Windows SMTC integration displaying real-time music metadata, album artwork, timeline progress bar, playback controls, and lyrics UI overlay.
- **📝 Live Countdown To-Do List**: Task manager featuring real-time countdown timers and automatic notification pop-ups when task deadlines arrive.
- **📁 Drag-and-Drop File Shelf**: Quick file/folder container. Drag items in, click to execute, or drag items out anywhere to remove them automatically from the shelf.
- **⚙️ Auto Startup**: Integrates seamlessly into Windows Startup to launch quietly on boot.

---

## 🛠️ Tech Stack & Architecture

- **Framework**: WPF (Windows Presentation Foundation) on .NET 8
- **Architecture**: Multi-page pager pattern (`INotchModule`)
- **APIs & System Interop**:
  - `GlobalSystemMediaTransportControlsSessionManager` (SMTC) for media session monitoring.
  - `kernel32.dll` P/Invoke for hardware power state retrieval.
  - Windows Registry auto-start integration.

---

## 🚀 Getting Started

### Prerequisites

- Windows 10 (v1903+) or Windows 11 (64-bit)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) *(Only required if building from source)*

### Installation & Run

1. Clone this repository:
   ```bash
   git clone [https://github.com/USERNAME/Pnotch.git](https://github.com/USERNAME/Pnotch.git)
   cd Pnotch
  RUN CODE:
  dotnet run
  BUILD .EXE:
  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
  
