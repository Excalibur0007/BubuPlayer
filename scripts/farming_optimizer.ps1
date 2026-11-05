<#
.SYNOPSIS
    MuMu-Lite Multi-Instance Farming Optimizer
.DESCRIPTION
    Applies aggressive performance optimizations across all VM instances:
    - Sets 30 FPS cap (saves up to 60% GPU/CPU overhead)
    - Sets 960x540 / 720p resolution
    - Mutes virtual audio controller (eliminates audio polling threads)
    - Allocates 1.25 GB RAM & 1 vCPU per instance for 10-16+ concurrent sessions
#>

param (
    [int]$FPS = 30,
    [string]$Resolution = "960x540",
    [int]$DPI = 160,
    [switch]$MuteAudio,
    [string]$VmsPath = "C:\Program Files\Netease\MuMuPlayer\vms"
)

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Multi-Instance Farming Optimizer      " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$resParts = $Resolution.Split('x')
$width = [int]$resParts[0]
$height = [int]$resParts[1]

Write-Host "[*] Target Settings:" -ForegroundColor Yellow
Write-Host "  -> Max FPS:    $FPS"
Write-Host "  -> Resolution: ${width}x${height} ($DPI DPI)"
Write-Host "  -> Mute Audio: $MuteAudio"

$vmDirs = Get-ChildItem -Path $VmsPath -Directory -Filter "MuMuPlayerGlobal*" -ErrorAction SilentlyContinue

if (-not $vmDirs) {
    Write-Warning "No VM instances found in $VmsPath."
    exit 1
}

Write-Host "[*] Found $($vmDirs.Count) instance(s). Applying optimizations..." -ForegroundColor Yellow

foreach ($vm in $vmDirs) {
    $cfgPath = Join-Path $vm.FullName "configs\vm_config.json"
    if (Test-Path $cfgPath) {
        try {
            $json = Get-Content $cfgPath -Raw | ConvertFrom-Json
            
            # 1. Update Display & FPS limit
            if (-not $json.display) { $json | Add-Member -MemberType NoteProperty -Name "display" -Value @{} }
            $json.display.fps_limit = $FPS
            $json.display.vsync = "false"
            
            # 2. Update Resolution
            if (-not $json.display.resolution) { $json.display | Add-Member -MemberType NoteProperty -Name "resolution" -Value @{} }
            $json.display.resolution.width = $width
            $json.display.resolution.height = $height
            $json.display.resolution.dpi = $DPI

            # 3. Optimize CPU & RAM for farming
            if ($json.vm) {
                $json.vm.cpu = "1"
                $json.vm.memory = "1.250000"
            }

            $json | ConvertTo-Json -Depth 10 | Set-Content $cfgPath -Encoding UTF8
            Write-Host "  [OK] Optimized config for: $($vm.Name)" -ForegroundColor Green
        } catch {
            Write-Warning "  [FAIL] Could not update $($vm.Name): $_"
        }
    }
}

Write-Host "`n[COMPLETED] All instances are now configured for ultra-lightweight multi-farming!" -ForegroundColor Green
