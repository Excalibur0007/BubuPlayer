<#
.SYNOPSIS
    KernelSU LKM & ksud Integrator for Android 12 (x86_64)
.DESCRIPTION
    Injects KernelSU into Android 12 emulator instances using the LKM (Loadable Kernel Module)
    architecture and installs the KernelSU Manager APK for stealth root governance.
#>

param (
    [string]$AdbPath = "C:\Program Files\Netease\MuMuPlayer\nx_main\adb.exe",
    [string]$TargetDevice = "127.0.0.1:7555",
    [string]$KsuKoPath = "",
    [string]$KsudPath = "",
    [string]$KsuManagerApk = ""
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   KernelSU Android 12 Integrator        " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

if (-not (Test-Path $AdbPath)) {
    Write-Error "ADB executable not found at: $AdbPath"
    exit 1
}

Write-Host "[*] Connecting to emulator target: $TargetDevice..." -ForegroundColor Yellow
& $AdbPath connect $TargetDevice

# Verify root shell access
$rootCheck = & $AdbPath -s $TargetDevice shell "id -u"
if ($rootCheck.Trim() -ne "0") {
    Write-Host "[!] Acquiring ADB root..." -ForegroundColor Yellow
    & $AdbPath -s $TargetDevice root
    Start-Sleep -Seconds 2
    & $AdbPath connect $TargetDevice
}

# Inspect Android Version and Kernel
$androidVersion = (& $AdbPath -s $TargetDevice shell "getprop ro.build.version.release").Trim()
$kernelVersion = (& $AdbPath -s $TargetDevice shell "uname -r").Trim()
Write-Host "[*] Detected Android Version: $androidVersion" -ForegroundColor Green
Write-Host "[*] Detected Kernel Version:  $kernelVersion" -ForegroundColor Green

Write-Host "[*] Remounting system partitions as read-write..." -ForegroundColor Yellow
& $AdbPath -s $TargetDevice remount

# 1. Inject ksud daemon
Write-Host "[1/4] Checking and configuring ksud daemon..." -ForegroundColor Yellow
$ksudCheck = & $AdbPath -s $TargetDevice shell "which ksud"
if (-not $ksudCheck) {
    if ($KsudPath -and (Test-Path $KsudPath)) {
        & $AdbPath -s $TargetDevice push $KsudPath /system/bin/ksud
        & $AdbPath -s $TargetDevice shell "chmod 0755 /system/bin/ksud"
        & $AdbPath -s $TargetDevice shell "chcon u:object_r:ksud_exec:s0 /system/bin/ksud"
        Write-Host "  -> ksud binary installed successfully." -ForegroundColor Green
    } else {
        Write-Host "  -> Note: Provide -KsudPath to automatically push custom ksud binary." -ForegroundColor Cyan
    }
} else {
    Write-Host "  -> ksud already present in system image." -ForegroundColor Green
}

# 2. Inject ksu.ko Loadable Kernel Module
Write-Host "[2/4] Verifying KernelSU LKM (ksu.ko)..." -ForegroundColor Yellow
if ($KsuKoPath -and (Test-Path $KsuKoPath)) {
    & $AdbPath -s $TargetDevice push $KsuKoPath /vendor/lib/modules/ksu.ko
    & $AdbPath -s $TargetDevice shell "insmod /vendor/lib/modules/ksu.ko"
    Write-Host "  -> ksu.ko module inserted into running kernel." -ForegroundColor Green
} else {
    Write-Host "  -> Checking active KernelSU status in kernel..." -ForegroundColor Cyan
    $ksuStatus = & $AdbPath -s $TargetDevice shell "cat /proc/version"
    Write-Host "  -> Kernel info: $ksuStatus"
}

# 3. Disable sponsored launcher inside guest
Write-Host "[3/4] Debloating Android guest apps..." -ForegroundColor Yellow
& $AdbPath -s $TargetDevice shell "pm disable-user --user 0 com.mumu.launcher" -ErrorAction SilentlyContinue
& $AdbPath -s $TargetDevice shell "pm disable-user --user 0 com.mumu.store" -ErrorAction SilentlyContinue
Write-Host "  -> Disabled com.mumu.launcher and com.mumu.store inside Android." -ForegroundColor Green

# 4. Install KernelSU Manager APK
Write-Host "[4/4] Installing KernelSU Manager App..." -ForegroundColor Yellow
if ($KsuManagerApk -and (Test-Path $KsuManagerApk)) {
    & $AdbPath -s $TargetDevice install -r $KsuManagerApk
    Write-Host "  -> KernelSU Manager APK installed!" -ForegroundColor Green
} else {
    Write-Host "  -> Download the latest KernelSU / KernelSU-Next Manager from official GitHub releases." -ForegroundColor Cyan
}

Write-Host "`n[COMPLETED] KernelSU setup routine finished on $TargetDevice!" -ForegroundColor Green
