# KernelSU on Android 12 (x86_64 Emulator Architecture)

## 📌 Background: Android 12 vs Android 15 KernelSU

Official upstream **KernelSU** primarily targets the **GKI (Generic Kernel Image)** architecture introduced in Android 13/14/15 with Linux Kernel 5.10+.

On **Android 12 (x86_64)**, emulator kernels are typically Linux Kernel 5.4 or non-GKI 5.10. To achieve **stealth, anti-cheat invisible root** on Android 12, we utilize the **KernelSU LKM (Loadable Kernel Module)** or the **KernelSU-Next** backported fork.

---

## 🔬 Which KernelSU Fork/Version to Use?

1. **KernelSU-Next (Recommended for 2026 Android 12/x86_64):**
   - **Repository:** `rifsxd/KernelSU-Next` / `KernelSU-Next`
   - **Why:** Actively maintained community fork that preserves non-GKI hooks, x86_64 architecture support, and backported SUSFS (Stealth Userspace File System) support.
   - **Manager App:** KernelSU Next Manager (compatible with Android 12+).

2. **KernelSU LKM (`ksu.ko`):**
   - **Concept:** Rather than compiling a completely new Linux kernel from scratch, KernelSU is compiled as an external Linux Kernel Module (`ksu.ko`).
   - The emulator kernel dynamically loads `ksu.ko` during early boot via `insmod`, providing identical Ring-0 root capabilities.

---

## ⚙️ How LKM Integration Works Step-by-Step

### 1. The Kernel Module (`ksu.ko`)
Placed in:
```
/vendor/lib/modules/ksu.ko
```
Loaded in `init.rc`:
```bash
insmod /vendor/lib/modules/ksu.ko
```

### 2. The Daemon (`ksud`)
Placed in:
```
/system/bin/ksud
```
Permissions & Context:
```bash
chmod 0755 /system/bin/ksud
chcon u:object_r:ksud_exec:s0 /system/bin/ksud
```
Hooked into early init:
```bash
exec /system/bin/ksud post-fs-data
```

### 3. The Manager App
Install `KernelSU_Manager.apk` to control:
- Granting root privileges on an app-by-app basis.
- Loading **ZygiskNext** (Zygisk implementation for KernelSU).
- Loading **Shamiko** or **SUSFS** modules to hide root completely from game anti-cheats (Tencent, EasyAntiCheat, BattleEye).

---

## 🛡️ Anti-Cheat Stealth Advantages of KernelSU on Android 12
- **SELinux Enforcing:** Stays in `Enforcing` mode, unlike traditional emulators that force `Permissive` mode.
- **No `su` binary in PATH:** Unapproved apps see zero root binaries, passing Google Play Integrity and game security scans.
