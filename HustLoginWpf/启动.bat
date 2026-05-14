@echo off
title HUST 校园网登录

dotnet --list-runtimes 2>nul | findstr "Microsoft.NETCore.App 10." >nul 2>&1
if %errorlevel% neq 0 (
    echo 未检测到 .NET 10 运行时，即将打开微软官方下载页面...
    echo 请下载安装 "Desktop Runtime 10.0.x" x64 版本后重新运行。
    start https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0
    pause
    exit /b 1
)

start "" "%~dp0HustLogin.exe"
