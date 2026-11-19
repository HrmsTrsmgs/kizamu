using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Backend.AspNetCore.Api.Tests;

using static Kizamu.Tests.TestCancellation;

public sealed class TimeRecordEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    static readonly DateTimeOffset 早い時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 遅い時刻 = new(2026, 8, 1, 11, 0, 0, TimeSpan.Zero);

    static readonly RSA googleKey = RSA.Create(2048);
    readonly HttpClient publicKeyClient = new(new PublicKeyHandler());
    readonly FakeTimeProvider timeProvider;
    readonly WebApplicationFactory<Program> configuredFactory;
    readonly HttpClient client;

    public TimeRecordEndpointTests(WebApplicationFactory<Program> factory)
    {
        timeProvider = new FakeTimeProvider();
        configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<TimeProvider>(timeProvider);
                services.RemoveAll<GoogleIdTokenVerifier>();
                services.AddSingleton(new GoogleIdTokenVerifier("kizamu-client", publicKeyClient));
            }));
        client = configuredFactory.CreateClient();
    }

    public static TheoryData<string?> 同一オリジンを示さないヘッダー値 => ["same-site", "cross-site", "none", "unknown", (string?)null];

    [Theory]
    [InlineData("GET", "/api/time-records")]
    [InlineData("POST", "/api/time-records/start")]
    public async Task Bearer以外の認証方式では401が返されます(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "token");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task 未認証で記録一覧を取得すると401が返されます()
    {
        using var response = await client.GetAsync("/api/time-records", CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task 未認証で記録を開始すると401が返されます()
    {
        using var request = 同一オリジンの未認証POST要求を用意する("/api/time-records/start");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task 未認証で記録を開始すると記録は追加されません()
    {
        using var request = 同一オリジンの未認証POST要求を用意する("/api/time-records/start");

        using var response = await client.SendAsync(request, CancellationToken);

        (await 記録一覧を取得する()).Should().BeEmpty();
    }

    [Fact]
    public async Task 未認証で記録を終了すると401が返されます()
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);
        using var request = 同一オリジンの未認証POST要求を用意する($"/api/time-records/{startedRecord.Id}/end");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task 未認証で記録を終了すると記録は変更されません()
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);
        using var request = 同一オリジンの未認証POST要求を用意する($"/api/time-records/{startedRecord.Id}/end");

        using var response = await client.SendAsync(request, CancellationToken);

        (await 記録一覧を取得する()).Should().Equal(startedRecord);
    }

    [Fact]
    public async Task 他の利用者の記録を終了すると404が返されます()
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻, "first-user");
        timeProvider.SetUtcNow(遅い時刻);
        using var request = 同一オリジンのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end", "second-user");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task 他の利用者の記録を終了すると所有者の記録は変更されません()
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻, "first-user");
        timeProvider.SetUtcNow(遅い時刻);
        using var request = 同一オリジンのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end", "second-user");

        using var response = await client.SendAsync(request, CancellationToken);

        (await 記録一覧を取得する("first-user")).Should().Equal(startedRecord);
    }

    [Fact]
    public async Task 二人が開始した記録の一覧にはそれぞれ本人の記録だけが返されます()
    {
        var firstRecord = await 開始済みの記録を用意する(早い時刻, "first-user");
        var secondRecord = await 開始済みの記録を用意する(遅い時刻, "second-user");
        (await 記録一覧を取得する("first-user")).Should().Equal(firstRecord);
        (await 記録一覧を取得する("second-user")).Should().Equal(secondRecord);
    }

    [Fact]
    public async Task 記録を開始すると201と作業記録のJSONが返されます()
    {
        timeProvider.SetUtcNow(早い時刻);

        using var request = 同一オリジンのPOST要求を用意する("/api/time-records/start");
        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>(CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().NotBe(Guid.Empty);
        body.StartedAt.Should().Be(早い時刻);
        body.EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task 記録を終了すると200と終了した作業記録のJSONが返されます()
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);

        using var request = 同一オリジンのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end");
        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>(CancellationToken);
        body.Should().NotBeNull();
        body.Id.Should().Be(startedRecord.Id);
        body.StartedAt.Should().Be(早い時刻);
        body.EndedAt.Should().Be(遅い時刻);
    }

    [Fact]
    public async Task 開始して終了した記録の一覧を取得すると200とJSON配列が返されます()
    {
        var recordId = await 終了済みの記録を用意する(早い時刻, 遅い時刻);

        using var response = await 認証済みの一覧要求を送る();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse[]>(CancellationToken);
        body.Should().Equal(
            new TimeRecordResponse(recordId, 早い時刻, 遅い時刻));
    }

    [Theory]
    [MemberData(nameof(同一オリジンを示さないヘッダー値))]
    public async Task SecFetchSiteが同一オリジンを示さない開始要求では403が返されます(string? secFetchSite)
    {
        using var request = 認証済みのPOST要求を用意する("/api/time-records/start", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(同一オリジンを示さないヘッダー値))]
    public async Task SecFetchSiteが同一オリジンを示さない開始要求では記録が追加されません(string? secFetchSite)
    {
        using var request = 認証済みのPOST要求を用意する("/api/time-records/start", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);

        (await 記録一覧を取得する()).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(同一オリジンを示さないヘッダー値))]
    public async Task SecFetchSiteが同一オリジンを示さない終了要求では403が返されます(string? secFetchSite)
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);
        using var request = 認証済みのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(同一オリジンを示さないヘッダー値))]
    public async Task SecFetchSiteが同一オリジンを示さない終了要求では記録が終了されません(string? secFetchSite)
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);
        using var request = 認証済みのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);

        (await 記録一覧を取得する()).Should().Equal(startedRecord);
    }

    async Task<TimeRecordResponse[]?> 記録一覧を取得する(string user = "google-user")
    {
        using var response = await 認証済みの一覧要求を送る(user);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<TimeRecordResponse[]>(CancellationToken);
    }

    async Task<HttpResponseMessage> 認証済みの一覧要求を送る(string user = "google-user")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/time-records");
        request.Headers.Authorization = 認証情報を用意する(user);
        return await client.SendAsync(request, CancellationToken);
    }

    static AuthenticationHeaderValue 認証情報を用意する(string user = "google-user")
    {
        var now = DateTimeOffset.UtcNow;
        var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = "endpoint-test-key" }));
        var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = "https://accounts.google.com",
            aud = "kizamu-client",
            sub = user,
            iat = now.AddHours(-2).ToUnixTimeSeconds(),
            exp = now.AddHours(1).ToUnixTimeSeconds()
        }));
        var content = $"{header}.{payload}";
        var signature = googleKey.SignData(Encoding.ASCII.GetBytes(content), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return new AuthenticationHeaderValue("Bearer", $"{content}.{Encode(signature)}");
    }

    static string Encode(ReadOnlySpan<byte> bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    sealed class PublicKeyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var parameters = googleKey.ExportParameters(false);
            var jwks = JsonSerializer.Serialize(new
            {
                keys = new[] { new { kty = "RSA", alg = "RS256", use = "sig", kid = "endpoint-test-key", n = Encode(parameters.Modulus), e = Encode(parameters.Exponent) } }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jwks, Encoding.UTF8, "application/json")
            });
        }
    }

    static HttpRequestMessage 同一オリジンの未認証POST要求を用意する(string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        return request;
    }

    static HttpRequestMessage 同一オリジンのPOST要求を用意する(string requestUri, string user = "google-user") =>
        認証済みのPOST要求を用意する(requestUri, "same-origin", user);

    static HttpRequestMessage 認証済みのPOST要求を用意する(string requestUri, string? secFetchSite, string user = "google-user")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = 認証情報を用意する(user);
        if (secFetchSite is not null)
            request.Headers.Add("Sec-Fetch-Site", secFetchSite);
        return request;
    }

    async Task<TimeRecordResponse> 開始済みの記録を用意する(DateTimeOffset startedAt, string user = "google-user")
    {
        timeProvider.SetUtcNow(startedAt);
        using var request = 同一オリジンのPOST要求を用意する("/api/time-records/start", user);
        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>(CancellationToken);
        body.Should().NotBeNull();
        return body;
    }

    async Task<Guid> 終了済みの記録を用意する(DateTimeOffset startedAt, DateTimeOffset endedAt)
    {
        var startedRecord = await 開始済みの記録を用意する(startedAt);
        timeProvider.SetUtcNow(endedAt);
        using var request = 同一オリジンのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end");
        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return startedRecord.Id;
    }

    public void Dispose()
    {
        client.Dispose();
        configuredFactory.Dispose();
        publicKeyClient.Dispose();
    }

    sealed record TimeRecordResponse(
        Guid Id,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt);
}
