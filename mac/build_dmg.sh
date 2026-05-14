#!/bin/bash
# 构建 HUST 校园网登录 macOS DMG 安装包
# 需要: pip3 install py2app
set -e

APP_NAME="HUST Login"
VERSION="2.0.0"
DMG_NAME="HustLogin-${VERSION}.dmg"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
DIST_DIR="$SCRIPT_DIR/dist"

echo "=== 构建 HUST 校园网登录 macOS DMG ==="
echo ""

# 1. Clean
echo "[1/5] 清理旧构建..."
rm -rf "$BUILD_DIR" "$DIST_DIR"

# 2. Install deps
echo "[2/5] 安装依赖..."
pip3 install --quiet py2app rumps

# 3. Build .app
echo "[3/5] 构建 .app..."
cd "$SCRIPT_DIR"
python3 setup.py py2app --no-strip 2>&1 | tail -3

# 4. Verify
APP_PATH="$DIST_DIR/$APP_NAME.app"
if [ ! -d "$APP_PATH" ]; then
    echo "❌ .app 构建失败"
    exit 1
fi
echo "[4/5] .app 构建完成: $APP_PATH"

# 5. Create DMG
echo "[5/5] 创建 DMG..."
DMG_TMP="$BUILD_DIR/dmg"
mkdir -p "$DMG_TMP"
cp -R "$APP_PATH" "$DMG_TMP/"
ln -s /Applications "$DMG_TMP/Applications"

hdiutil create -volname "$APP_NAME" \
    -srcfolder "$DMG_TMP" \
    -ov -format UDZO \
    "$SCRIPT_DIR/$DMG_NAME" 2>&1 | tail -1

# Cleanup
rm -rf "$DMG_TMP"

echo ""
echo "✅ 完成！"
echo "   DMG: $SCRIPT_DIR/$DMG_NAME"
echo "   .app: $APP_PATH"
echo ""
echo "   分发方式:"
echo "   1. DMG 直接发给用户（拖到 Applications 即可）"
echo "   2. .app ZIP 压缩后上传 GitHub Release"
