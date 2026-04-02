@echo off
echo ========================================
echo   Camera Pro - Full Setup & Build
echo ========================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] .NET SDK already installed:
    dotnet --version
    echo.
    goto :build
)

echo [INFO] .NET SDK not found. Installing...
echo.

REM Try Chocolatey
where choco >nul 2>&1
if %errorlevel% equ 0 (
    echo Installing via Chocolatey...
    choco install dotnet-sdk -y
    if %errorlevel% equ 0 goto :verify
)

REM Try Winget
where winget >nul 2>&1
if %errorlevel% equ 0 (
    echo Installing via Winget...
    winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
    if %errorlevel% equ 0 goto :verify
)

REM Manual download
echo.
echo ========================================
echo MANUAL INSTALLATION REQUIRED
echo ========================================
echo.
echo Please download and install .NET 8 SDK:
echo.
echo 1. Open this link in your browser:
echo    https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo 2. Click "Download .NET SDK x64" 
echo    (v8.0.xxx - NOT the runtime)
echo.
echo 3. Run the installer
echo.
echo 4. Restart this computer
echo.
echo 5. Run this script again
echo.
pause
exit /b 1

:verify
echo.
echo Verifying installation...
call refreshenv >nul 2>&1
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Installation verification failed
    echo Please restart your computer and try again
    pause
    exit /b 1
)

:build
echo.
echo ========================================
echo Building Camera Pro...
echo ========================================
echo.

cd /d "%~dp0CameraPro"

echo Step 1: Restoring packages...
dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] Restore failed
    pause
    exit /b 1
)

echo.
echo Step 2: Building...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

echo.
echo Step 3: Publishing...
dotnet publish CameraPro.App\CameraPro.App.csproj -c Release -r win-x64 --self-contained -o publish
if %errorlevel% neq 0 (
    echo [ERROR] Publish failed
    pause
    exit /b 1
)

echo.
echo ========================================
echo BUILD SUCCESSFUL!
echo ========================================
echo.
echo EXE Location:
echo   %~dp0CameraPro\publish\CameraPro.exe
echo.
echo Press any key to open the folder...
explorer "%~dp0CameraPro\publish"
pause
