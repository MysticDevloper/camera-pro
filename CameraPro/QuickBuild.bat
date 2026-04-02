@echo off
cd /d "%~dp0CameraPro"
dotnet build -c Release
dotnet publish CameraPro.App\CameraPro.App.csproj -c Release -r win-x64 --self-contained -o publish
pause
