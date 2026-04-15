@echo off
chcp 65001 >nul
title ZBoxingServer - 编译并启动

echo ========================================
echo   ZBoxingServer 一键编译并启动
echo ========================================
echo.

:: 1. 先杀掉可能残留的服务端进程（占用DLL）
echo [1/3] 检查残留进程...
for /f "tokens=2" %%i in ('tasklist /fi "imagename eq dotnet.exe" /fi "windowtitle eq ZBoxingServer*" /fo csv /nh 2^>nul') do (
    taskkill /PID %%~i /F >nul 2>&1
)

:: 2. 编译Hotfix（改动最频繁的部分）
echo [2/3] 编译 Hotfix...
cd /d "%~dp0"
dotnet build DotNet\Hotfix\DotNet.Hotfix.csproj -c Release
if %errorlevel% neq 0 (
    echo.
    echo !! 编译失败，请检查错误 !!
    pause
    exit /b 1
)

echo.
echo [3/3] 启动服务端...
echo ----------------------------------------
cd /d "%~dp0Bin"
dotnet App.dll --Console=1

:: 服务端退出后暂停（方便看日志）
echo.
echo 服务端已退出
pause
