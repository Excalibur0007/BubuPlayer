@echo off
echo ===================================================
echo     Compiling BubuPlayer Suite (.exe)
echo ===================================================

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist %CSC% (
    echo [ERROR] C# Compiler (csc.exe) not found at: %CSC%
    pause
    exit /b 1
)

if not exist "bin" mkdir bin

echo [*] 1/2 Compiling Web Installer (BubuPlayerSetup.exe)...
%CSC% /target:winexe /out:"bin\BubuPlayerSetup.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll "src\installer\BubuPlayerSetup.cs"

echo [*] 2/2 Compiling Main Emulator Player (BubuPlayer.exe)...
%CSC% /target:winexe /out:"bin\BubuPlayer.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll "src\player\BubuPlayerApp.cs"

if %errorlevel% equ 0 (
    echo.
    echo ===================================================
    echo [SUCCESS] All executables compiled cleanly!
    echo   - bin\BubuPlayerSetup.exe (Web Installer)
    echo   - bin\BubuPlayer.exe      (Main Emulator Application)
    echo ===================================================
) else (
    echo.
    echo [ERROR] Compilation failed.
)
pause
