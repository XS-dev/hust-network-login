using System.IO;
using System.Net.Http;

namespace HustLogin.Services;

public class LoginService : IDisposable
{
    public void Dispose() => _client.Dispose();
    private readonly HttpClient _client;

    public LoginService()
    {
        _client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        });
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("hust-network-login");
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var resp = await _client.GetAsync("http://www.baidu.com");
        var body = await resp.Content.ReadAsStringAsync();

        if (!body.Contains("/eportal/index.jsp") &&
            !body.Contains("<script>top.self.location.href='http://"))
            return true;

        var portalIp = Extract(body, "<script>top.self.location.href='http://", "/eportal/index.jsp");
        var mac = Extract(body, "mac=", "&t=");
        var encrypted = EncryptService.Encrypt($"{password}>{mac}");
        var queryString = Extract(body, "/eportal/index.jsp?", "'</script>\r\n");
        var queryStringEnc = Uri.EscapeDataString(queryString);

        var postBody = $"userId={username}&password={encrypted}&service=&queryString={queryStringEnc}&passwordEncrypt=true";

        var postResp = await _client.PostAsync(
            $"http://{portalIp}/eportal/InterFace.do?method=login",
            new StringContent(postBody, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));

        var loginResult = await postResp.Content.ReadAsStringAsync();
        return loginResult.Contains("success");
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
