using System.Numerics;

namespace HustLogin.Services;

public static class EncryptService
{
    private const int E = 65537;
    private const string ModulusHex =
        "94dd2a8675fb779e6b9f7103698634cd400f27a154afa67af6166a43fc264172" +
        "22a79506d34cacc7641946abda1785b7acf9910ad6a0978c91ec84d40b71d289" +
        "1379af19ffb333e7517e390bd26ac312fe940c340466b4a5d4af1d65c3b594407" +
        "8f96a1a51a5a53e4bc302818b7c9f63c4a1b07bd7d874cef1c3d4b2f5eb7871";
    private static readonly BigInteger N = new(
        Convert.FromHexString(ModulusHex),
        isUnsigned: true,
        isBigEndian: true);

    public static string Encrypt(string password)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var c = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
        var result = BigInteger.ModPow(c, E, N);
        return Convert.ToHexString(result.ToByteArray(isUnsigned: true, isBigEndian: true))
            .ToLowerInvariant()
            .PadLeft(256, '0');
    }
}
