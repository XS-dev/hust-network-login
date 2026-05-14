class HustNetworkLogin < Formula
  desc "HUST campus network auto-login — macOS menu bar app"
  homepage "https://github.com/XS-dev/hust-network-login"
  url "https://github.com/XS-dev/hust-network-login/archive/refs/tags/v2.0.0.tar.gz"
  sha256 "PLACEHOLDER" # 首次发布后: curl -sL <url> | shasum -a 256
  license "MIT"
  version "2.0.0"

  depends_on "python@3.13"

  resource "rumps" do
    url "https://files.pythonhosted.org/packages/source/r/rumps/rumps-0.4.0.tar.gz"
    sha256 "PLACEHOLDER"
  end

  def install
    venv = libexec/"venv"
    system "python3.13", "-m", "venv", venv
    system "#{venv}/bin/pip", "install", resource("rumps")

    # Install script to libexec
    libexec.install "mac/HustLogin.py"

    # Create bin wrapper
    (bin/"hust-login").write <<~SH
      #!/bin/bash
      exec "#{venv}/bin/python3" "#{libexec}/HustLogin.py"
    SH
    chmod 0755, bin/"hust-login"
  end

  def caveats
    <<~EOS
      启动: 终端运行 hust-login
      开机自启: 系统设置 → 通用 → 登录项 → 添加 #{opt_bin}/hust-login
      配置文件: ~/.hust-login/my.conf (账号/密码/间隔)
    EOS
  end

  test do
    system "#{venv}/bin/python3", "-c", "import rumps"
  end
end
