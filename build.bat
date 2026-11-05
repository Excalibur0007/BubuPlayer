@echo off
echo ===================================================
echo     Compiling BubuPlayer Suite with Full Branding
echo ===================================================

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set ICON="assets\app.ico"
set ASM="src\AssemblyInfo.cs"

if not exist %CSC% (
    echo [ERROR] C# Compiler (csc.exe) not found at: %CSC%
    pause
    exit /b 1
)

if not exist "bin" mkdir bin

echo [*] 1/2 Compiling Web Installer (BubuPlayerSetup.exe)...
%CSC% /target:winexe /win32icon:%ICON% /out:"bin\BubuPlayerSetup.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll %ASM% "src\installer\BubuPlayerSetup.cs"

echo [*] 2/2 Compiling Main Emulator Player (BubuPlayer.exe)...
%CSC% /target:winexe /win32icon:%ICON% /out:"bin\BubuPlayer.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll %ASM% "src\player\BubuPlayerApp.cs"

if %errorlevel% equ 0 (
    echo.
    echo ===================================================
    echo [SUCCESS] Branded BubuPlayer Suite compiled cleanly!
    echo   - bin\BubuPlayerSetup.exe (Branded with app.ico & version 1.2.0.0)
    echo   - bin\BubuPlayer.exe      (Branded with app.ico & version 1.2.0.0)
    echo ===================================================
) else (
    echo.
    echo [ERROR] Compilation failed.
)
pause
