# Multi-Instance Farming Optimization Blueprint

## 🎯 Objective
Run 10 to 16+ simultaneous emulator accounts on a standard 16GB–32GB gaming PC with zero frame stutter, low thermal output, and maximum 24/7 stability.

---

## 📊 Resource Math: Android 12 vs Android 15

| Configuration | RAM per Instance | Max on 16GB PC | Max on 32GB PC |
|---|---|---|---|
| **Stock MuMu 15 (Android 15)** | ~2.8 GB – 3.0 GB | 3 – 4 Instances | 7 – 8 Instances |
| **MuMu-Lite (Android 12)** | **~1.2 GB – 1.4 GB** | **8 – 10 Instances** | **16 – 20 Instances** |

---

## ⚡ The 4 Golden Rules of Multi-Farming

### Rule 1: Cap FPS at 30
- In farming/botting, 60 or 120 FPS is completely wasted GPU/CPU compute.
- Capping FPS at 30 cuts GPU power consumption by **~60%** and prevents thermal throttling.

### Rule 2: Resolution 960x540 at 160 DPI
- Standard 1080p rendering forces the emulator to process $1920 \times 1080 = 2,073,600$ pixels per frame.
- 960x540 requires only $518,400$ pixels — a **75% reduction in pixel shading workload**.

### Rule 3: Batch Mute Audio
- Virtual audio emulators poll Windows sound devices continuously for each active instance.
- Muting instances stops the audio polling loop, saving ~3-5% CPU cycles per instance ($10 \text{ instances} \times 4\% = 40\%$ total CPU saved).

### Rule 4: Disable Google Play Auto-Sync & Real-Time Protect
- In each instance:
  1. Go to `Settings -> Accounts -> Google` -> Disable Sync for Contacts, Calendar, Drive.
  2. Open Play Store -> `Play Protect` -> Gear icon -> Turn off `Scan apps with Play Protect`.
  3. This eliminates background disk I/O bottlenecks.

---

## 🛠️ Automated Setup via Script
Run the automated optimizer:
```powershell
.\scripts\farming_optimizer.ps1 -FPS 30 -Resolution "960x540" -MuteAudio $true
```
