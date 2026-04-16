@echo off
title ZBoxingServer Build and Run

echo ========================================
echo   ZBoxingServer Build and Run
echo ========================================
echo.

echo [1/3] Building Hotfix...
cd /d "%~dp0"
dotnet build DotNet\Hotfix\DotNet.Hotfix.csproj -c Release
if %errorlevel% neq 0 (
    echo.
    echo !! BUILD FAILED !!
    pause
    exit /b 1
)

echo.
echo [2/3] Build OK. Starting server...
echo ----------------------------------------
cd /d "%~dp0Bin"
dotnet App.dll --Console=1

echo.
echo Server stopped.
pause
