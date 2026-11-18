using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests.Authentication;

public sealed class GoogleIdTokenVerifierTests : IClassFixture<GoogleIdTokenVerifierTests.SigningKey>, IDisposable
{
    readonly RSA googleKey;
    readonly HttpClient publicKeyClient;
    readonly GoogleIdTokenVerifier tested;

    public GoogleIdTokenVerifierTests(SigningKey signingKey)
    {
        googleKey = signingKey.Key;
        publicKeyClient = new(new PublicKeyHandler(googleKey));
        tested = new("kizamu-client", publicKeyClient);
    }

    [Theory]
    [InlineData("https://accounts.google.com")]
    [InlineData("accounts.google.com")]
    public async Task 有効なGoogleトークンでは更新やメール変更後も同じsubで本人を識別する(string issuer)
    {
        var 初回トークン = CreateToken(issuer: issuer, email: "before@example.test");
        var 更新後トークン = CreateToken(issuer: issuer, email: "after@example.test");

        (await tested.VerifyAsync(初回トークン)).Should().Be("google-user");
        (await tested.VerifyAsync(更新後トークン)).Should().Be("google-user");
    }

    [Fact]
    public async Task メールが同じでもsubが異なる利用者を区別する()
    {
        (await tested.VerifyAsync(CreateToken(subject: "first-user"))).Should().Be("first-user");
        (await tested.VerifyAsync(CreateToken(subject: "second-user"))).Should().Be("second-user");
    }

    [Theory]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("missing-sub")]
    [InlineData("malformed")]
    public async Task 不正なGoogleトークンから本人識別子を返さない(string invalidPart)
    {
        using var attackerKey = RSA.Create(2048);
        var token = invalidPart switch
        {
            "signature" => CreateToken(signingKey: attackerKey),
            "issuer" => CreateToken(issuer: "https://attacker.example.test"),
            "audience" => CreateToken(audience: "another-client"),
            "expired" => CreateToken(expired: true),
            "missing-sub" => CreateToken(subject: null),
            "malformed" => "not-a-jwt",
            _ => throw new ArgumentOutOfRangeException(nameof(invalidPart))
        };

        (await tested.VerifyAsync(token)).Should().BeNull();
    }

    string CreateToken(
        string? subject = "google-user",
        string issuer = "https://accounts.google.com",
        string audience = "kizamu-client",
        string email = "user@example.test",
        bool expired = false,
        RSA? signingKey = null)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new Dictionary<string, object>
        {
            ["iss"] = issuer,
            ["aud"] = audience,
            ["email"] = email,
            ["iat"] = now.AddHours(-2).ToUnixTimeSeconds(),
            ["exp"] = now.AddHours(expired ? -1 : 1).ToUnixTimeSeconds()
        };
        if (subject is not null)
            payload["sub"] = subject;
        var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = "test-key", typ = "JWT" }));
        var content = $"{header}.{Encode(JsonSerializer.SerializeToUtf8Bytes(payload))}";
        var signature = (signingKey ?? googleKey).SignData(Encoding.ASCII.GetBytes(content), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{content}.{Encode(signature)}";
    }

    static string Encode(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public void Dispose() => publicKeyClient.Dispose();

    // テスト間で同じ公開鍵を使い、検証ライブラリの公開鍵キャッシュとも両立させます。
    public sealed class SigningKey : IDisposable
    {
        public RSA Key { get; } = RSA.Create(2048);

        public void Dispose() => Key.Dispose();
    }

    // 外部通信だけを差し替え、JWTの検証は実際の検証器へ渡します。
    sealed class PublicKeyHandler(RSA key) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var parameters = key.ExportParameters(false);
            var jwks = JsonSerializer.Serialize(new
            {
                keys = new[] { new { kty = "RSA", alg = "RS256", use = "sig", kid = "test-key", n = Encode(parameters.Modulus), e = Encode(parameters.Exponent) } }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jwks, Encoding.UTF8, "application/json")
            });
        }
    }
}
