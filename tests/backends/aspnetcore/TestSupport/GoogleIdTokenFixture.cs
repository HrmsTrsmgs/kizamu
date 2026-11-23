using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Kizamu.Backend.AspNetCore.TestSupport;

/// <summary>公開鍵キャッシュと両立するよう、テストクラス内で同じ署名鍵を使います。</summary>
public abstract class GoogleIdTokenFixture(string keyId) : IDisposable
{
    readonly RSA key = RSA.Create(2048);

    /// <summary>テスト用の公開鍵を返す HTTP クライアントを作ります。</summary>
    public HttpClient CreatePublicKeyClient() => new(new PublicKeyHandler(key, keyId));

    /// <summary>テスト用の鍵 ID と指定されたクレームで ID トークンを作ります。</summary>
    public string CreateToken<TClaims>(TClaims claims, string? type = null, RSA? signingKey = null)
    {
        var header = type is null
            ? JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = keyId })
            : JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = keyId, typ = type });
        var content = $"{Encode(header)}.{Encode(JsonSerializer.SerializeToUtf8Bytes(claims))}";
        var signature = (signingKey ?? key).SignData(Encoding.ASCII.GetBytes(content), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{content}.{Encode(signature)}";
    }

    static string Encode(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public void Dispose()
    {
        key.Dispose();
        GC.SuppressFinalize(this);
    }

    // 外部通信だけを差し替え、JWTの検証は実際の検証器へ渡します。
    sealed class PublicKeyHandler(RSA key, string keyId) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var parameters = key.ExportParameters(false);
            var jwks = JsonSerializer.Serialize(new
            {
                keys = new[]
                {
                    new { kty = "RSA", alg = "RS256", use = "sig", kid = keyId, n = Encode(parameters.Modulus), e = Encode(parameters.Exponent) }
                }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jwks, Encoding.UTF8, "application/json")
            });
        }
    }
}
