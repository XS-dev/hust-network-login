#!/bin/bash
# HUST 校园网登录 — macOS 安装脚本
set -e

echo "=== HUST 校园网登录 - macOS 安装 ==="
echo ""

# Check Python
if ! command -v python3 &>/dev/null; then
    echo "[ERROR] 未找到 python3。请先安装 Python 3:"
    echo "  brew install python3"
    echo "  或从 https://www.python.org/downloads/ 下载"
    exit 1
fi

echo "[OK] Python3: $(python3 --version)"

# Install rumps
echo "[*] 安装依赖 rumps..."
pip3 install --user rumps

# Create config dir
mkdir -p ~/.hust-login

# Ask for credentials
echo ""
echo "请输入校园网账号和密码："
read -p "账号: " username
read -sp "密码: " password
echo ""
read -p "检测间隔秒数 [60]: " interval
interval=${interval:-60}

echo "$username" > ~/.hust-login/my.conf
echo "$password" >> ~/.hust-login/my.conf
echo "$interval" >> ~/.hust-login/my.conf

chmod 600 ~/.hust-login/my.conf

echo ""
echo "[OK] 配置已保存到 ~/.hust-login/my.conf"
echo ""

# Create wrapper script
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cat > /usr/local/bin/hust-login << WRAPPER
#!/bin/bash
python3 "$SCRIPT_DIR/HustLogin.py" &
WRAPPER
chmod +x /usr/local/bin/hust-login

echo "[OK] 安装完成!"
echo ""
echo "使用方法:"
echo "  终端运行:  hust-login"
echo "  开机自启:  系统设置 → 通用 → 登录项 → 添加 /usr/local/bin/hust-login"
echo ""
echo "  或手动运行: python3 $SCRIPT_DIR/HustLogin.py"
