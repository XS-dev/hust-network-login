# HUST 校园网自动登录工具

华中科技大学校园网自动登录客户端，支持 **Windows（WPF 图形界面）** 和 **macOS（菜单栏应用）**。

> 本项目登录协议及加密算法参考自 [black-binary/hust-network-login](https://github.com/black-binary/hust-network-login)（极简 Rust 原版）。

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

从 [Releases](https://github.com/XS-dev/hust-network-login/releases) 下载自包含版本 `HustLogin.exe`（约 72MB），放入任意文件夹，双击运行。

> 此版本内置完整 .NET 10 运行时，适用于任何 Windows 10 x64 及以上系统。

### 方式二：框架依赖版本（体积小，需 .NET 10 运行时）

下载 `publish-fd.zip`（exe 仅 159KB），解压后双击 `启动.bat`。

> 如未安装 .NET 10 运行时，启动脚本会自动检测并打开微软官方下载页面。安装 "Desktop Runtime 10.0.x" x64 版本后即可运行。

### 方式三：Rust 原版命令行（跨平台/嵌入式）

从原项目 [black-binary/hust-network-login](https://github.com/black-binary/hust-network-login) 下载对应平台的静态链接可执行文件（约 400KB），命令行运行：

```shell
./hust-network-login ./my.conf
```

## 配置文件

`my.conf` 位于程序同目录，格式为 3 行：

```
学号/工号
密码
检测间隔秒数（可选，默认 60）
```

也可以在 GUI 界面中直接填写并保存。

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

hust-network-login-src/    # 原始 Rust 项目（black-binary/hust-network-login）
gui.bat                    # 一键启动脚本
login.bat                  # 原始命令行启动脚本
```

### macOS 版本（菜单栏应用）

**方式一：Homebrew 安装（推荐）**

```bash
brew tap XS-dev/tap https://github.com/XS-dev/hust-network-login
brew install hust-network-login
hust-login
```

**方式二：下载 DMG**

从 [Releases](https://github.com/XS-dev/hust-network-login/releases) 下载 `.dmg`，拖入 Applications，双击运行。

**方式三：源码运行**

```bash
cd mac
bash setup.sh      # 一键安装配置
python3 HustLogin.py
```

**构建 DMG（开发者）：**

```bash
cd mac
pip3 install py2app rumps
bash build_dmg.sh   # 生成 dist/HUST Login.app 和 .dmg
```

## 编译

### Windows WPF 版本

```bash
# 安装 .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

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

### Rust 原版

```bash
cargo build --release
strip ./target/release/hust-network-login
```

交叉编译推荐使用 `cross`：

```bash
cargo install cross
cross build --release --target mips-unknown-linux-musl
```

## 登录原理

1. GET `http://www.baidu.com` 检测是否已被校园网劫持
2. 若被重定向到认证页，解析 portal IP、MAC 地址、query string
3. 使用内置 RSA 公钥加密密码（密码 + ">" + MAC）
4. POST 到校园网认证接口完成登录

## 技术栈

### WPF 版
- .NET 10 WPF / C# 13
- System.Drawing（图标渲染）
- System.Net.Http（HTTP 请求）
- System.Numerics（大数 RSA 加密）

### Rust 原版
- Rust / minreq
- num-bigint（RSA 加密）

## 致谢

- [black-binary/hust-network-login](https://github.com/black-binary/hust-network-login) — 原始 Rust 实现，提供了登录协议与加密算法

## License

MIT
