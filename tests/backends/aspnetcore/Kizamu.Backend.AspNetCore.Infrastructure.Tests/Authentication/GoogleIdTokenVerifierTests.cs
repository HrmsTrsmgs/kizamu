using System.Security.Cryptography;
using AwesomeAssertions;
using Google.Apis.Auth;
using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;
using Kizamu.Backend.AspNetCore.TestSupport;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests.Authentication;

public sealed class GoogleIdTokenVerifierTests : IClassFixture<GoogleIdTokenVerifierTests.SigningKey>, IDisposable
{
    readonly SigningKey googleTokens;
    readonly HttpClient publicKeyClient;
    readonly GoogleIdTokenVerifier tested;

    public GoogleIdTokenVerifierTests(SigningKey signingKey)
    {
        googleTokens = signingKey;
        publicKeyClient = signingKey.CreatePublicKeyClient();
        tested = new("kizamu-client", publicKeyClient);
    }

    [Theory]
    [InlineData("https://accounts.google.com")]
    [InlineData("accounts.google.com")]
    public async Task GoogleのIDトークンを更新してメールアドレスが変わっても同じsubを本人識別子として取得できます(string issuer)
    {
        var 初回トークン = CreateToken("google-user", issuer: issuer, email: "before@example.test");
        var 更新後トークン = CreateToken("google-user", issuer: issuer, email: "after@example.test");

        (await tested.VerifyAsync(初回トークン)).Should().Be("google-user");
        (await tested.VerifyAsync(更新後トークン)).Should().Be("google-user");
    }

    [Fact]
    public async Task GoogleのIDトークンのメールアドレスが同じでもsubが異なれば本人識別子も異なります()
    {
        const string 同じメールアドレス = "user@example.test";
        var 最初のトークン = CreateToken("first-user", email: 同じメールアドレス);
        var 別のトークン = CreateToken("second-user", email: 同じメールアドレス);

        (await tested.VerifyAsync(最初のトークン)).Should().Be("first-user");
        (await tested.VerifyAsync(別のトークン)).Should().Be("second-user");
    }

    [Fact]
    public async Task Googleの公開鍵で署名を検証できないIDトークンからは本人識別子を取得できません()
    {
        using var attackerKey = RSA.Create(2048);

        (await tested.VerifyAsync(CreateToken("google-user", signingKey: attackerKey))).Should().BeNull();
    }

    [Fact]
    public async Task 発行者がGoogle以外のIDトークンからは本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken("google-user", issuer: "https://attacker.example.test"))).Should().BeNull();
    }

    [Fact]
    public async Task 別のクライアント向けのGoogleのIDトークンからは本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken("google-user", audience: "another-client"))).Should().BeNull();
    }

    [Fact]
    public async Task 有効期限を過ぎたGoogleのIDトークンからは本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken("google-user", expired: true))).Should().BeNull();
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
        var verifier = 有効期限と現在時刻を指定した検証器を用意する(有効期限, 有効期限.AddTicks(ticksFromExpiration));

        (await verifier.VerifyAsync("verified-token-placeholder")).Should().Be(expectedSubject);
    }

    [Fact]
    public async Task GoogleのIDトークンにsubがなければ本人識別子を取得できません()
    {
        (await tested.VerifyAsync(CreateToken(subject: null))).Should().BeNull();
    }

    [Theory]
    [InlineData(123, null)]
    [InlineData("", null)]
    [InlineData("00123", "00123")]
    [InlineData("2026-01-01T00:00:00Z", "2026-01-01T00:00:00Z")]
    public async Task Googleのsubが空でないJSON文字列のときだけ元の値を本人識別子として取得できます(
        object subject,
        string? expectedSubject)
    {
        (await tested.VerifyAsync(CreateToken(subject))).Should().Be(expectedSubject);
    }

    [Fact]
    public async Task JWT形式でない文字列をGoogleのIDトークンとして検証しても本人識別子を取得できません()
    {
        (await tested.VerifyAsync("not-a-jwt")).Should().BeNull();
    }

    static GoogleIdTokenVerifier 有効期限と現在時刻を指定した検証器を用意する(
        DateTimeOffset expiresAt,
        DateTimeOffset utcNow) =>
        new(
            verifySignedToken: _ => Task.FromResult(new JsonWebToken.Payload
            {
                Subject = "google-user",
                ExpirationTimeSeconds = expiresAt.ToUnixTimeSeconds()
            }),
            timeProvider: new FixedTimeProvider(utcNow));

    string CreateToken(
        object? subject,
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
        return googleTokens.CreateToken(payload, type: "JWT", signingKey: signingKey);
    }

    public void Dispose() => publicKeyClient.Dispose();

    sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    public sealed class SigningKey() : GoogleIdTokenFixture("test-key") { }
}
