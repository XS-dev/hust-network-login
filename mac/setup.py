"""
py2app setup — 将 HustLogin.py 打包为独立 .app
macOS 上运行: python3 setup.py py2app
"""
from setuptools import setup

APP = ["HustLogin.py"]
DATA_FILES = []
OPTIONS = {
    "argv_emulation": False,
    "plist": {
        "CFBundleName": "HUST Login",
        "CFBundleDisplayName": "HUST 校园网登录",
        "CFBundleIdentifier": "com.hust.network-login",
        "CFBundleVersion": "2.0.0",
        "CFBundleShortVersionString": "2.0.0",
        "LSUIElement": True,  # 菜单栏应用，不显示 Dock 图标
        "NSHumanReadableCopyright": "MIT License. 参考 black-binary/hust-network-login",
    },
    "packages": ["rumps"],
    "includes": ["urllib", "encodings.idna"],
}

setup(
    app=APP,
    name="HUST Login",
    data_files=DATA_FILES,
    options={"py2app": OPTIONS},
    setup_requires=["py2app"],
)
