# 🌙 Pnotch

> A sleek, high-utility desktop notch for Windows, styled with a **Persona 3 Reload** aesthetic and dynamic **Chiikawa** vector facial expressions.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?style=flat&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green.svg)

---

## ✨ Key Features

- **🌙 Persona 3 Reload Aesthetic**: Built with Velvet Navy (`#080F20`), Sky Cyan (`#00A6FF` / `#50C8FF`), and Persona Gold (`#FFC72C`) accents.
- **🐰 Dynamic Chiikawa Faces**: Native WPF vector animations with expressions that change dynamically (Normal, Blink, Cute 🥺, and Happy).
- 🙈 **Auto-Hide Strip Mode**: Automatically shrinks into a subtle 3px top strip (100x3px) when obstructed by active foreground windows to keep your display clutter-free.
- 📱 **Multi-Module Pager System**:
  - 🎵 **Media Player**: Real-time media playback control (Spotify, YouTube, web browsers) complete with album art downscaling.
  - ✅ **Todo List & Alarm**: Task management with automated shoulder-tap notifications when deadlines hit.
  - 📁 **File Shelf**: Quick drag-and-drop temporary storage area.
  - 📋 **Clipboard Manager**: Interactive clipboard history log.
- ⚡ **Ultra-Low RAM Footprint**: Memory-optimized (~15–30 MB RAM) using Win32 WorkingSet compaction upon idling.
- 🔋 **Real-Time Status Header**: Live clock and responsive battery status monitor.
---

<img width="1919" height="1199" alt="image" src="https://github.com/user-attachments/assets/ad7eaa8e-248f-472b-8d02-29382b8a0890" /><img width="1919" height="1149" alt="image" src="https://github.com/user-attachments/assets/b130d812-6e03-4a61-aaf0-78edda2ca57d" /><img width="1919" height="1149" alt="image" src="https://github.com/user-attachments/assets/6a0906aa-8970-46e0-a3ef-e62a7af79766" /><img width="1916" height="1149" alt="image" src="https://github.com/user-attachments/assets/37c181cc-0cc2-4fa1-a10a-f72fd4ac7e22" /><img width="1917" height="1149" alt="image" src="https://github.com/user-attachments/assets/fbb831e5-82c7-43ff-a8a9-db8878d95632" />






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
  
