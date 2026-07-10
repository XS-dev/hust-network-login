using System.IO;
using System.Text;
using System.Net.Http;

namespace HustLogin.Services;

public class LoginService : IDisposable
{
    private readonly HttpClient _client;
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "error.log");

    public LoginService()
    {
        _client = new HttpClient(new HttpClientHandler
        {
            // The portal response itself contains the information needed for login.
            AllowAutoRedirect = false
        })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("hust-network-login");
        _client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
    }

    public void Dispose() => _client.Dispose();

    private static void LogError(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] ERROR {msg}\n"); }
        catch { }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        string body;
        try
        {
            using var response = await _client.GetAsync("http://www.baidu.com");
            body = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            LogError($"探测百度失败: {ex.Message}");
            throw;
        }

        if (!body.Contains("/eportal/index.jsp") &&
            !body.Contains("<script>top.self.location.href='http://"))
            return true;

        var portalIp = Extract(body, "<script>top.self.location.href='http://", "/eportal/index.jsp");
        var mac = Extract(body, "mac=", "&t=");
        var queryString = Extract(body, "/eportal/index.jsp?", "'</script>\r\n");
        var encrypted = EncryptService.Encrypt($"{password}>{mac}");
        var postBody = $"userId={username}&password={encrypted}&service=&queryString={Uri.EscapeDataString(queryString)}&passwordEncrypt=true";

        try
        {
            using var content = new StringContent(postBody, Encoding.UTF8);
            content.Headers.ContentType = new("application/x-www-form-urlencoded")
            {
                CharSet = "UTF-8"
            };
            using var postResponse = await _client.PostAsync(
                $"http://{portalIp}/eportal/InterFace.do?method=login",
                content);
            var loginResult = await postResponse.Content.ReadAsStringAsync();
            if (!loginResult.Contains("success"))
                LogError($"登录被拒: {(loginResult.Length > 300 ? loginResult[..300] : loginResult)}");
            return loginResult.Contains("success");
        }
        catch (Exception ex)
        {
            LogError($"登录 POST 失败: {ex.Message}");
            throw;
        }
    }

    private static string Extract(string text, string prefix, string suffix)
    {
        var l = text.IndexOf(prefix);
        var r = text.IndexOf(suffix);
        if (l != -1 && r != -1 && l + prefix.Length < r)
            return text[(l + prefix.Length)..r];
        throw new InvalidDataException("extract failed");
    }
}
