#!/usr/bin/env python3
"""
HUST 校园网登录 — macOS 菜单栏版本
依赖: rumps (pip install rumps)
配置文件: ~/.hust-login/my.conf  或  /etc/hust-network-login.conf
"""

import os
import sys
import threading
import time
import urllib.request
import urllib.parse

try:
    import rumps
except ImportError:
    print("请先安装 rumps: pip3 install rumps")
    sys.exit(1)

# ── encrypt ────────────────────────────────────────────────────
E = 65537
N = int(
    "94dd2a8675fb779e6b9f7103698634cd400f27a154afa67af6166a43fc264172"
    "22a79506d34cacc7641946abda1785b7acf9910ad6a0978c91ec84d40b71d289"
    "1379af19ffb333e7517e390bd26ac312fe940c340466b4a5d4af1d65c3b594407"
    "8f96a1a51a5a53e4bc302818b7c9f63c4a1b07bd7d874cef1c3d4b2f5eb7871",
    16,
)


def encrypt_pass(password: str) -> str:
    c = int.from_bytes(password.encode(), "big")
    result = pow(c, E, N)
    return format(result, "0256x")


# ── HTTP ───────────────────────────────────────────────────────
def http_get(url, timeout=10):
    req = urllib.request.Request(url, headers={"User-Agent": "hust-network-login"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read().decode(errors="replace")


def http_post(url, body, timeout=10):
    data = body.encode()
    req = urllib.request.Request(
        url,
        data=data,
        headers={
            "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
            "Accept": "*/*",
            "User-Agent": "hust-network-login",
        },
    )
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read().decode(errors="replace")


def extract(text, prefix, suffix):
    l = text.find(prefix)
    r = text.find(suffix)
    if l != -1 and r != -1 and l + len(prefix) < r:
        return text[l + len(prefix) : r]
    raise ValueError("extract failed")


def login(username, password):
    resp = http_get("http://www.baidu.com")
    if "/eportal/index.jsp" not in resp and "<script>top.self.location.href='http://" not in resp:
        return True
    portal_ip = extract(resp, "<script>top.self.location.href='http://", "/eportal/index.jsp")
    mac = extract(resp, "mac=", "&t=")
    enc = encrypt_pass(f"{password}>{mac}")
    qs = extract(resp, "/eportal/index.jsp?", "'</script>\r\n")
    qs_enc = urllib.parse.quote(qs, safe="")
    body = f"userId={username}&password={enc}&service=&queryString={qs_enc}&passwordEncrypt=true"
    resp = http_post(f"http://{portal_ip}/eportal/InterFace.do?method=login", body)
    return "success" in resp


# ── config ─────────────────────────────────────────────────────
CONF_DIR = os.path.expanduser("~/.hust-login")
CONF_PATH = os.path.join(CONF_DIR, "my.conf")


def load_config():
    paths = [CONF_PATH, "/etc/hust-network-login.conf", "/etc/hust-network-login/config"]
    for p in paths:
        try:
            with open(p) as f:
                lines = [l.strip() for l in f if l.strip()]
                if len(lines) >= 2:
                    interval = 60
                    if len(lines) >= 3:
                        try:
                            interval = max(5, min(86400, int(lines[2].strip())))
                        except ValueError:
                            pass
                    return lines[0], lines[1], interval
        except Exception:
            continue
    return None, None, 60


def save_config(username, password, interval):
    os.makedirs(CONF_DIR, exist_ok=True)
    with open(CONF_PATH, "w") as f:
        f.write(f"{username}\n{password}\n{interval}\n")


# ── menu bar app ────────────────────────────────────────────────
class HustApp(rumps.App):
    def __init__(self):
        super().__init__("HUST", title="📶")
        self.username, self.password, self.interval = load_config()
        self.online: bool | None = None
        self.last_check: str = ""
        self.countdown: int = self.interval
        self.checking: bool = False
        self._build_menu()
        self._start_timer()

    def _build_menu(self):
        self.status_item = rumps.MenuItem("状态: 等待检测...", callback=None)
        self.last_item = rumps.MenuItem("上次检测: --", callback=None)
        self.countdown_item = rumps.MenuItem(f"下次检测: {self.countdown}s", callback=None)
        self.menu = [
            self.status_item,
            self.last_item,
            self.countdown_item,
            None,  # separator
            rumps.MenuItem("立即检测", callback=self._check_now),
            rumps.MenuItem("偏好设置...", callback=self._preferences),
            None,
            rumps.MenuItem("关于", callback=self._about),
            rumps.MenuItem("退出", callback=self._quit),
        ]

    def _start_timer(self):
        rumps.Timer(self._tick, 1).start()
        threading.Thread(target=self._do_check, daemon=True).start()

    def _tick(self, _):
        self.countdown -= 1
        label = f"下次检测: {self.countdown}s"
        if self.online is False:
            label = f"快速重试: {self.countdown}s"
        self.countdown_item.title = label
        if self.countdown <= 0:
            self.countdown = 10 if self.online is False else self.interval
            self.countdown_item.title = "检测中..."
            threading.Thread(target=self._do_check, daemon=True).start()

    def _check_now(self, _):
        self.countdown = self.interval
        threading.Thread(target=self._do_check, daemon=True).start()

    def _do_check(self):
        if self.checking:
            return
        self.checking = True
        try:
            self.title = "🟠"
            self.status_item.title = "状态: 检测中..."
            if self.username and self.password:
                ok = login(self.username, self.password)
                self.online = ok
                self.title = "🟢" if ok else "🔴"
                self.status_item.title = "状态: 已连接" if ok else "状态: 未连接"
            else:
                self.title = "⚪"
                self.status_item.title = "状态: 请配置账号密码"
        except Exception as e:
            self.title = "🔴"
            self.status_item.title = f"状态: 错误 — {e}"
        finally:
            self.last_check = time.strftime("%H:%M:%S")
            self.last_item.title = f"上次检测: {self.last_check}"
            self.checking = False

    def _preferences(self, _):
        window = rumps.Window(
            title="偏好设置",
            message="输入账号、密码和检测间隔",
            default_text=f"{self.username or ''}\n{self.password or ''}\n{self.interval}",
            dimensions=(260, 120),
        )
        window.icon = None
        response = window.run()
        if response.clicked:
            lines = [l.strip() for l in response.text.strip().split("\n") if l.strip()]
            if len(lines) >= 2:
                self.username = lines[0]
                self.password = lines[1]
                if len(lines) >= 3:
                    try:
                        self.interval = max(5, min(86400, int(lines[2])))
                    except ValueError:
                        self.interval = 60
                save_config(self.username, self.password, self.interval)
                self.countdown = self.interval
                self.countdown_item.title = f"下次检测: {self.countdown}s"

    def _about(self, _):
        rumps.alert(
            title="HUST 校园网登录",
            message=(
                "华中科技大学校园网自动登录工具\n"
                "macOS 菜单栏版本 v1.0\n\n"
                "参考项目: black-binary/hust-network-login"
            ),
        )

    def _quit(self, _):
        rumps.quit_application()


if __name__ == "__main__":
    HustApp().run()
