# BubuPlayer: Ultra-Lightweight, Ads-Free Android 12 Gaming Emulator

[![GitHub Release](https://img.shields.io/badge/Release-v1.0.0-blue.svg)](https://github.com/Excalibur0007/BubuPlayer/releases)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux-brightgreen.svg)]()
[![Android Version](https://img.shields.io/badge/Android-12%20(64--bit)-orange.svg)]()
[![Root](https://img.shields.io/badge/Root-KernelSU%20(Stealth)-purple.svg)]()

**BubuPlayer** is an ultra-fast, bloat-free Android emulator built upon MuMu's high-speed VirtIO hypervisor and direct GPU translation bridge, paired with a debloated **Android 12 (64-bit)** system image and **KernelSU LKM** stealth root. 

Fine-tuned specifically for **Multi-Instance Farming** (10–16+ concurrent sessions) and high-refresh competitive gaming (120 FPS).

---

## 🌟 Key Features

- **Web Setup Installer (`BubuPlayerSetup.exe`):** Send a tiny 16 KB installer to clients. It connects to GitHub Releases, downloads required files, debloats the environment, and drops a desktop shortcut automatically.
- **Android 12 Sweet-Spot:** 100% compatibility with modern 2026+ mobile titles while slashing RAM usage from ~3.0 GB down to **1.2 GB per instance**.
- **KernelSU (LKM / Next):** Anti-cheat invisible root operating at Kernel Ring-0 (SELinux Enforcing, zero exposed `su` binaries, ZygiskNext & Shamiko ready).
- **100% Ads & Telemetry Free:** Stripped of NetEase's heavy Chromium webview (`CefView`), background updaters (`MuMuNxUpdater`), downloader daemons (`aria2`), and statistics collectors (`MuMuStatisticsReporter`).
- **MuMu Core Engine Preservation:** Keeps the exact same proprietary GPU translation pipeline (`VAddressDevice` / Vulkan), 120 FPS frame timing, and raw input keymapper.
- **Dual Platform Support:** Runs on **Windows** (MuMuNx Headless VirtIO) and **Linux** (native QEMU-KVM with VirtIO graphics).

---

## 📁 Repository Structure

```
BubuPlayer/
├── bin/
│   └── BubuPlayerSetup.exe             # Compiled Windows Web Installer ready to distribute
├── build.bat                           # One-click native C# compiler script
├── src/
│   └── installer/
│       └── BubuPlayerSetup.cs          # Full source code for the Web Installer GUI
├── core/
│   ├── vm_config_template.json         # Lean 1-core / 1.25GB RAM hardware profile
│   └── hosts_adblock.txt               # Adware and telemetry blocking rules
├── scripts/
│   ├── setup_windows.ps1               # Automated host debloater & cleaner
│   ├── integrate_kernelsu.ps1          # KernelSU LKM patcher for Android 12
│   ├── farming_optimizer.ps1           # 10+ Multi-instance batch optimizer (30 FPS/540p)
│   └── setup_linux.sh                  # Linux QEMU-KVM VirtIO runner script
└── docs/
    ├── KERNELSU_ANDROID12_GUIDE.md     # Deep dive into KernelSU LKM on x86_64
    └── MULTI_FARMING_GUIDE.md          # Multi-instance farming setup and RAM calculations
```

---

## 🚀 How Client Distribution Works (Zero Server Cost)

Instead of hosting a 2.5 GB heavy setup on paid servers:
1. **You provide clients with `bin/BubuPlayerSetup.exe`** (or let them download it from your GitHub Releases).
2. When the client runs `BubuPlayerSetup.exe`:
   - It checks system VT-x virtualization.
   - Automatically downloads the payload directly from GitHub Releases (`Excalibur0007/BubuPlayer`).
   - If a base MuMu installation is detected, it instantly decouples and debloats it locally without consuming bandwidth.
   - Injects the ad-blocking rules into the Windows hosts file.
   - Drops `BubuPlayer.lnk` on the desktop.
   - Client clicks **Launch Now** and enjoys 120 FPS ad-free gaming.

---

## 🔨 How to Recompile the Installer

To recompile `BubuPlayerSetup.exe` from source on any Windows machine:
```cmd
build.bat
```
*(Uses the built-in Microsoft .NET Framework C# compiler `csc.exe` — zero additional IDE or software installation required).*

---

## 🛡️ KernelSU Setup on Android 12

For complete technical documentation on how KernelSU operates via Loadable Kernel Modules (`ksu.ko` + `ksud`) on Android 12 x86_64, refer to:
📖 [docs/KERNELSU_ANDROID12_GUIDE.md](docs/KERNELSU_ANDROID12_GUIDE.md)

---

## 🚜 Multi-Farming Optimization

To configure all emulator instances for 24/7 low-load farming (30 FPS, 960x540 resolution, muted audio polling):
```powershell
Set-ExecutionPolicy Bypass -Scope Process -Force
.\scripts\farming_optimizer.ps1 -FPS 30 -Resolution "960x540" -MuteAudio $true
```
📖 [docs/MULTI_FARMING_GUIDE.md](docs/MULTI_FARMING_GUIDE.md)

---

## 📜 License
Created for personal performance enhancement, research, and clean-room emulator deployment. All third-party trademarks belong to their respective owners.
