using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Google.Apis.Auth;
using Google.Apis.Json;
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
    public async Task GoogleのIDトークンを更新してメールアドレスが変わっても同じsubを本人識別子として取得できます(string issuer)
    {
        var 初回トークン = CreateToken(issuer: issuer, email: "before@example.test");
        var 更新後トークン = CreateToken(issuer: issuer, email: "after@example.test");

        (await tested.VerifyAsync(初回トークン)).Should().Be("google-user");
        (await tested.VerifyAsync(更新後トークン)).Should().Be("google-user");
    }

    [Fact]
    public async Task GoogleのIDトークンのメールアドレスが同じでもsubが異なれば本人識別子も異なります()
    {
        (await tested.VerifyAsync(CreateToken(subject: "first-user"))).Should().Be("first-user");
        (await tested.VerifyAsync(CreateToken(subject: "second-user"))).Should().Be("second-user");
    }

    [Fact]
    public async Task Googleの公開鍵で署名を検証できないIDトークンからは本人識別子を取得できません()
    {
        using var attackerKey = RSA.Create(2048);

        (await tested.VerifyAsync(CreateToken(signingKey: attackerKey))).Should().BeNull();
    }

    [Fact]
    public async Task 発行者がGoogle以外のIDトークンからは本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken(issuer: "https://attacker.example.test"))).Should().BeNull();
    }

    [Fact]
    public async Task 別のクライアント向けのGoogleのIDトークンからは本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken(audience: "another-client"))).Should().BeNull();
    }

    [Fact]
    public async Task 有効期限を過ぎたGoogleのIDトークンからは本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken(expired: true))).Should().BeNull();
    }

    [Theory]
    [InlineData(-1, "google-user")]
    [InlineData(0, null)]
    [InlineData(1, null)]
    public async Task GoogleのIDトークンは有効期限に達するまで本人識別子を取得できます(
        long ticksFromExpiration,
        string? expectedSubject)
    {
        var 有効期限 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var verifier = new GoogleIdTokenVerifier(
            verifySignedToken: _ => Task.FromResult(new JsonWebToken.Payload
            {
                Subject = "google-user",
                ExpirationTimeSeconds = 有効期限.ToUnixTimeSeconds()
            }),
            timeProvider: new FixedTimeProvider(有効期限.AddTicks(ticksFromExpiration)));

        (await verifier.VerifyAsync("verified-token-placeholder")).Should().Be(expectedSubject);
    }

    [Fact]
    public async Task GoogleのIDトークンにsubがなければ本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken(subject: null))).Should().BeNull();
    }

    [Theory]
    [InlineData("123", null)]
    [InlineData("\"\"", null)]
    [InlineData("\"00123\"", "00123")]
    [InlineData("\"2026-01-01T00:00:00Z\"", "2026-01-01T00:00:00Z")]
    public async Task Googleのsubが空でないJSON文字列のときだけ元の値を本人識別子として取得できます(
        string subjectJson,
        string? expectedSubject)
    {
        var claimsJson = $$"""{"sub":{{subjectJson}},"exp":60}""";
        var verifier = new GoogleIdTokenVerifier(
            verifySignedToken: _ => Task.FromResult<JsonWebToken.Payload>(
                NewtonsoftJsonSerializer.Instance.Deserialize<GoogleIdentityPayload>(claimsJson)),
            timeProvider: new FixedTimeProvider(DateTimeOffset.UnixEpoch));

        (await verifier.VerifyAsync("verified-token-placeholder")).Should().Be(expectedSubject);
    }

    [Fact]
    public async Task JWT形式でない文字列をGoogleのIDトークンとして検証しても本人識別子を取得できません()
    {
        (await tested.VerifyAsync("not-a-jwt")).Should().BeNull();
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

    sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

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
