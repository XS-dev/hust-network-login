mod encrypt;
use std::fs;
use std::fs::OpenOptions;
use std::io::Write;
use std::time::Duration;
use std::{io, thread};

fn log_error(msg: &str) {
    let ts = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap_or_default();
    let entry = format!("[{}.{:03}] ERROR {}\n", ts.as_secs(), ts.subsec_millis(), msg);
    let _ = OpenOptions::new()
        .create(true)
        .append(true)
        .open("hust-network-login-error.log")
        .and_then(|mut f| f.write_all(entry.as_bytes()));
    eprintln!("ERROR: {}", msg);
}

fn extract<'a>(text: &'a str, prefix: &'a str, suffix: &'a str) -> io::Result<&'a str> {
    let left = text.find(prefix);
    let right = text.find(suffix);
    if let (Some(l), Some(r)) = (left, right) {
        if l + prefix.len() < r {
            return Ok(&text[l + prefix.len()..r]);
        }
    }
    Err(io::ErrorKind::InvalidData.into())
}

fn login(username: &str, password: &str) -> io::Result<()> {
    let resp = minreq::get("http://www.baidu.com")
        .with_timeout(10)
        .send()
        .map_err(|e| {
            let msg = format!("探测百度失败: {}", e);
            log_error(&msg);
            io::Error::new(io::ErrorKind::Other, msg)
        })?;
    let resp = resp.as_str().map_err(|e| {
        let msg = format!("百度响应非 UTF-8: {}", e);
        log_error(&msg);
        io::ErrorKind::InvalidData
    })?;

    if !resp.contains("/eportal/index.jsp")
        && !resp.contains("<script>top.self.location.href='http://")
    {
        return Ok(());
    }

    let portal_ip = extract(
        resp,
        "<script>top.self.location.href='http://",
        "/eportal/index.jsp",
    )
    .map_err(|_| {
        log_error("提取 portal_ip 失败: 前缀或后缀不匹配");
        io::Error::from(io::ErrorKind::InvalidData)
    })?;
    println!("portal ip: {}", portal_ip);

    let mac = extract(resp, "mac=", "&t=").map_err(|_| {
        log_error("提取 mac 地址失败: 'mac=' 或 '&t=' 分隔符不匹配");
        io::Error::from(io::ErrorKind::InvalidData)
    })?;
    println!("mac: {}", mac);

    let encrypt_pass = encrypt::encrypt_pass(format!("{}>{}", password, mac));

    let query_string = extract(resp, "/eportal/index.jsp?", "'</script>\r\n").map_err(|_| {
        log_error("提取 query_string 失败: 后缀 '\\'</script>\\r\\n' 不匹配，门户页面格式可能已变更");
        io::Error::from(io::ErrorKind::InvalidData)
    })?;
    println!("query_string: {}", query_string);

    let query_string = urlencoding::encode(query_string);

    let body = format!(
        "userId={}&password={}&service=&queryString={}&passwordEncrypt=true",
        username, encrypt_pass, query_string
    );

    let login_url = format!("http://{}/eportal/InterFace.do?method=login", portal_ip);

    let resp = minreq::post(login_url)
        .with_body(body)
        .with_header(
            "Content-Type",
            "application/x-www-form-urlencoded; charset=UTF-8",
        )
        .with_header("Accept", "*/*")
        .with_header("User-Agent", "hust-network-login")
        .with_timeout(10)
        .send()
        .map_err(|e| {
            let msg = format!("登录 POST 请求失败: {}", e);
            log_error(&msg);
            io::Error::new(io::ErrorKind::Other, msg)
        })?;

    let resp = resp.as_str().map_err(|e| {
        let msg = format!("登录响应非 UTF-8: {}", e);
        log_error(&msg);
        io::ErrorKind::InvalidData
    })?;

    println!("login resp: {}", resp);

    if resp.contains("success") {
        Ok(())
    } else {
        log_error(&format!("登录被拒: 响应不含 'success'。响应体: {}", &resp[..resp.len().min(500)]));
        Err(io::Error::from(io::ErrorKind::PermissionDenied))
    }
}

#[test]
fn login_test() {
    let _ = login("username", "password");
}

struct Config {
    username: String,
    password: String,
}

impl Config {
    fn validate_and_assemble(
        username: Option<&str>,
        password: Option<&str>,
    ) -> Result<Self, &'static str> {
        match (username, password) {
            (Some(_), None) => Err("missing password"),
            (None, Some(_)) => Err("missing username"),
            (None, None) => Err("missing username and password"),
            (Some(username), Some(password)) => Ok(Self {
                username: username.to_owned(),
                password: password.to_owned(),
            }),
        }
    }

    pub fn from_env() -> Option<Self> {
        println!("reading configuration from environment variables");
        let username = std::env::var("HUST_NETWORK_LOGIN_USERNAME")
            .inspect_err(|err| println!("failed to read environment variable: {err}"))
            .ok();
        let password = std::env::var("HUST_NETWORK_LOGIN_PASSWORD")
            .inspect_err(|err| println!("failed to read environment variable: {err}"))
            .ok();

        let result = Self::validate_and_assemble(
            username.as_ref().map(String::as_str),
            password.as_ref().map(String::as_str),
        )
        .inspect_err(|err| println!("invalid configuration: {err}"))
        .ok()?;

        Some(result)
    }

    pub fn from_file(path: &str) -> Option<Self> {
        println!("reading configuration from file: {path}");

        let raw = fs::read(&path)
            .inspect_err(|err| println!("failed to read from {path}: {err}"))
            .ok()?;

        let configuration = String::from_utf8(raw)
            .inspect_err(|err| println!("failed to parse content of {path}: {err}"))
            .ok()?;

        let mut lines = configuration.lines();
        let username = lines.next();
        let password = lines.next();
        let result = Self::validate_and_assemble(username, password)
            .inspect_err(|err| println!("invalid configuration: {err}"))
            .ok()?;

        Some(result)
    }

    pub fn from_args() -> Option<Self> {
        println!("reading configuration from arguments");

        let args = std::env::args();

        let path = args
            .skip(1) // skip executable path
            .last()
            .ok_or("at least 1 argument is required")
            .inspect_err(|err| println!("no configuration file specified: {err}"))
            .ok()?;

        Self::from_file(&path)
    }
}

fn main() {
    let config = Config::from_args()
        .or_else(Config::from_env)
        .or_else(|| Config::from_file("/etc/hust-network-login.conf"))
        .or_else(|| Config::from_file("/etc/hust-network-login/config"))
        .unwrap_or_else(|| {
            log_error("无法找到任何配置来源 (命令行参数/环境变量/配置文件)");
            panic!("no available configuration found")
        });

    loop {
        match login(&config.username, &config.password) {
            Ok(_) => {
                println!("login ok. awaiting...");
                thread::sleep(Duration::from_secs(15));
            }
            Err(e) => {
                log_error(&format!("登录循环出错: {}", e));
                thread::sleep(Duration::from_secs(1));
            }
        }
    }
}
