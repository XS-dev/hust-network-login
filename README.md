# HUST 校园网自动登录工具

华中科技大学校园网自动登录客户端，基于 WPF (.NET 10) 开发，支持图形界面与系统托盘后台运行。

> 本项目登录协议及加密算法参考自 [black-binary/hust-network-login](https://github.com/black-binary/hust-network-login)。

## 功能特性

- **图形界面** — 现代化 WPF 界面，实时显示连接状态
- **系统托盘** — 最小化到托盘后台运行，悬停显示状态，右键退出
- **自动检测** — 定时检测网络连接状态，断线自动重连
- **智能重试** — 在线时按设定间隔检测；断线后快速重试（每 10 秒）
- **网络变化感知** — 监听系统网络状态，WiFi 重连后 3 秒内即时检测
- **连接日志** — 滚动记录每次检测结果，方便排查问题
- **开机自启** — 可选注册到系统启动项
- **可配置间隔** — 检测间隔 5 秒 ~ 24 小时自由设定
- **轻量便携** — 单文件 exe，配置文件独立

## 下载与运行

### 方式一：自包含版本（推荐，无需安装任何运行时）

下载 `publish/HustLogin.exe`（约 72MB），放入任意文件夹，双击运行。

> 此版本内置完整 .NET 10 运行时，适用于任何 Windows 10 x64 及以上系统。

### 方式二：框架依赖版本（体积小，需 .NET 10 运行时）

下载 `publish-fd/` 整个文件夹（exe 仅 159KB），双击 `启动.bat`。

> 如未安装 .NET 10 运行时，启动脚本会自动打开微软官方下载页面。安装 "Desktop Runtime 10.0.x" x64 版本后即可运行。

## 配置说明

程序自动读取同目录下的 `my.conf` 文件，格式为 3 行：

```
学号/工号
密码
检测间隔秒数（可选，默认 60）
```

也可以在 GUI 界面中直接填写并保存，无需手动编辑配置文件。

## 项目结构

```
HustLoginWpf/              # WPF 主项目
├── Services/
│   ├── EncryptService.cs  # RSA 公钥加密
│   └── LoginService.cs    # HTTP 登录逻辑
├── Assets/
│   └── icon.svg           # 图标源文件
├── MainWindow.xaml        # 界面设计
├── MainWindow.xaml.cs     # 界面逻辑
├── 启动.bat               # 框架依赖版启动器
└── HustLogin.csproj       # 项目文件

hust-network-login-src/    # 原始 Rust 项目（参考）
gui.bat                    # 一键启动脚本
login.bat                  # 原始命令行启动脚本
```

## 编译

```bash
# 安装 .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

# 进入项目目录
cd HustLoginWpf

# 框架依赖版（小体积）
dotnet publish -c Release -r win-x64 -o publish-fd

# 自包含版（可移植）
dotnet publish -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:EnableCompressionInSingleFile=true \
  -o publish
```

## 登录原理

1. GET `http://www.baidu.com` 检测是否已被校园网劫持
2. 若被重定向到认证页，解析 portal IP、MAC 地址、query string
3. 使用内置 RSA 公钥加密密码（密码 + ">" + MAC）
4. POST 到校园网认证接口完成登录

## 技术栈

- .NET 10 WPF
- C# 13
- System.Drawing（图标渲染）
- System.Net.Http（HTTP 请求）
- System.Numerics（大数 RSA 加密）

## 致谢

- [black-binary/hust-network-login](https://github.com/black-binary/hust-network-login) — 原始 Rust 实现，提供了登录协议与加密算法

## License

MIT
