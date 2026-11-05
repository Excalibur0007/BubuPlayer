@echo off
echo ===================================================
echo     Compiling BubuPlayer Web Installer (.exe)
echo ===================================================

set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist %CSC% (
    echo [ERROR] C# Compiler (csc.exe) not found at: %CSC%
    pause
    exit /b 1
)

if not exist "bin" mkdir bin

%CSC% /target:winexe /out:"bin\BubuPlayerSetup.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll "src\installer\BubuPlayerSetup.cs"

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] BubuPlayerSetup.exe compiled cleanly to: bin\BubuPlayerSetup.exe
) else (
    echo.
    echo [ERROR] Compilation failed.
)
pause
