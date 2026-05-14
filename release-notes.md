# HUST 校园网登录 v2.0.0

基于 [black-binary/hust-network-login](https://github.com/black-binary/hust-network-login) 的现代化版本。

## 新特性

- **Windows WPF 图形界面** — 系统托盘、连接日志、开机自启、智能重试
- **macOS 菜单栏应用** — 原生菜单栏图标、状态显示、偏好设置
- **检测间隔可配置** — 5秒 ~ 24小时自由设定
- **网络变化即时感知** — WiFi 重连 3 秒内自动检测
- **断线快速重试** — 掉线后每 10 秒快速重试
- **双版本发布** — 自包含便携版（72MB，无需安装运行时）

## 下载

- **Windows 便携版**: `HustLogin-win-x64.exe` — 下载即用，支持 Win10+ x64
- **macOS 脚本版**: `mac-release.zip` — 解压后运行 `bash setup.sh`

## 配置

程序同目录放置 `my.conf`，3 行格式：
```
学号/工号
密码
检测间隔秒数（默认 60）
```

也可在 GUI 中直接填写保存。

## 致谢

- [black-binary/hust-network-login](https://github.com/black-binary/hust-network-login) — 原始 Rust 实现

## License

MIT
