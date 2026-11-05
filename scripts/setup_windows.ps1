<#
.SYNOPSIS
    MuMu-Lite Windows Host Debloater & Optimizer
.DESCRIPTION
    Stops and disables NetEase telemetry, background updaters, aria2 ad-downloaders,
    applies DNS/hosts ad blocking, and configures the environment for lightweight execution.
#>

param (
    [switch]$DebloatHost,
    [switch]$RestoreOriginal,
    [string]$MuMuPath = "C:\Program Files\Netease\MuMuPlayer"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   MuMu-Lite: Host Optimizer Script     " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsPrincipal]::Administrator)) {
    Write-Warning "Please run this script as Administrator to allow hosts file and process modifications."
    exit 1
}

$nxMain = Join-Path $MuMuPath "nx_main"
$hostsPath = "$env:SystemRoot\System32\drivers\etc\hosts"
$adblockFile = Join-Path $PSScriptRoot "..\core\hosts_adblock.txt"

if ($RestoreOriginal) {
    Write-Host "[*] Restoring original executables..." -ForegroundColor Yellow
    $targets = @("MuMuStatisticsReporter", "MuMuNxUpdater", "aria2")
    foreach ($t in $targets) {
        $bak = Join-Path $nxMain "$t.bak"
        $orig = Join-Path $nxMain "$t.exe"
        if (Test-Path $bak) {
            Rename-Item -Path $bak -NewName "$t.exe" -Force
            Write-Host "  -> Restored $t.exe" -ForegroundColor Green
        }
    }
    exit 0
}

if ($DebloatHost) {
    Write-Host "[1/3] Terminating bloatware & telemetry processes..." -ForegroundColor Yellow
    $procs = @("MuMuStatisticsReporter", "MuMuNxUpdater", "aria2", "MuMuWechatMiniGame")
    foreach ($p in $procs) {
        Get-Process -Name $p -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }

    Write-Host "[2/3] Renaming bloatware binaries to prevent auto-restart..." -ForegroundColor Yellow
    foreach ($p in $procs) {
        $exe = Join-Path $nxMain "$p.exe"
        $bak = Join-Path $nxMain "$p.bak"
        if (Test-Path $exe) {
            Rename-Item -Path $exe -NewName "$p.bak" -Force -ErrorAction SilentlyContinue
            Write-Host "  -> Disabled: $p.exe (Renamed to .bak)" -ForegroundColor Green
        }
    }

    Write-Host "[3/3] Injecting AdBlocker & Telemetry rules into Windows hosts file..." -ForegroundColor Yellow
    if (Test-Path $adblockFile) {
        $blockRules = Get-Content $adblockFile
        $currentHosts = Get-Content $hostsPath -Raw
        $rulesToAdd = @()

        foreach ($rule in $blockRules) {
            $trimmed = $rule.Trim()
            if ($trimmed -and -not $trimmed.StartsWith("#")) {
                if ($currentHosts -notmatch [regex]::Escape($trimmed)) {
                    $rulesToAdd += $trimmed
                }
            }
        }

        if ($rulesToAdd.Count -gt 0) {
            Add-Content -Path $hostsPath -Value "`r`n# MuMu-Lite Telemetry Blocker"
            Add-Content -Path $hostsPath -Value $rulesToAdd
            Write-Host "  -> Added $($rulesToAdd.Count) ad-block entries to hosts file." -ForegroundColor Green
        } else {
            Write-Host "  -> Telemetry blocker entries already present." -ForegroundColor Cyan
        }
    }

    Write-Host "`n[SUCCESS] Windows Host is now fully debloated and ad-free!" -ForegroundColor Green
}
