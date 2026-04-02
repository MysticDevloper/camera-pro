@echo off
echo ========================================
echo   Camera Pro - Build Script
echo ========================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK not found!
    echo.
    echo Please install .NET 8 SDK:
    echo 1. Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo 2. Run the installer
    echo 3. Restart this script
    echo.
    pause
    exit /b 1
)

echo [OK] .NET SDK found
dotnet --version
echo.

REM Navigate to project
cd /d "%~dp0CameraPro"
if %errorlevel% neq 0 (
    echo [ERROR] CameraPro folder not found!
    echo Expected at: %~dp0CameraPro
    pause
    exit /b 1
)

echo [OK] Project folder found
echo.

REM Restore packages
echo ========================================
echo Step 1: Restoring NuGet packages...
echo ========================================
dotnet restore
if %errorlevel% neq 0 (
    echo [ERROR] Restore failed!
    pause
    exit /b 1
)
echo.

REM Build
echo ========================================
echo Step 2: Building project...
echo ========================================
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)
echo.

REM Publish
echo ========================================
echo Step 3: Publishing application...
echo ========================================
dotnet publish CameraPro.App\CameraPro.App.csproj -c Release -r win-x64 --self-contained -o publish
if %errorlevel% neq 0 (
    echo [ERROR] Publish failed!
    pause
    exit /b 1
)
echo.

echo ========================================
echo SUCCESS!
echo ========================================
echo.
echo EXE Location:
echo   %~dp0CameraPro\publish\CameraPro.exe
echo.
echo Press any key to open the folder...
explorer "%~dp0CameraPro\publish"
pause
