@echo off
title HUST 校园网登录
chcp 65001 >nul

REM Check for .NET 10 runtime
dotnet --list-runtimes 2>nul | findstr "Microsoft.NETCore.App 10." >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo  ╔══════════════════════════════════════════╗
    echo  ║  未检测到 .NET 10 运行时                 ║
    echo  ╠══════════════════════════════════════════╣
    echo  ║  本程序需要 .NET 10 桌面运行时才能运行。  ║
    echo  ║                                          ║
    echo  ║  即将打开微软官方下载页面：               ║
    echo  ║  https://dotnet.microsoft.com/download    ║
    echo  ║                                          ║
    echo  ║  下载 "Desktop Runtime 10.0.x" x64 版本   ║
    echo  ║  安装后重新运行此脚本即可。               ║
    echo  ║                                          ║
    echo  ║  也可使用 publish 目录下的自包含版本，    ║
    echo  ║  无需额外安装任何运行时。                 ║
    echo  ╚══════════════════════════════════════════╝
    echo.
    start https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0
    pause
    exit /b 1
)

start "" "%~dp0HustLogin.exe"
exit /b 0
