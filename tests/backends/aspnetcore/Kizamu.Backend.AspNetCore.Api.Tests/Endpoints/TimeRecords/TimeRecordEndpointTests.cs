using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Backend.AspNetCore.Infrastructure.Authentication;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Backend.AspNetCore.Api.Tests.Endpoints.TimeRecords;

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
    public async Task Basic認証で記録の一覧取得や開始を要求すると401が返ります(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "token");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task 未認証で記録一覧を要求すると401が返ります()
    {
        using var response = await client.GetAsync("/api/time-records", CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task 未認証で記録の開始を要求すると401が返り一覧は空のままです()
    {
        using var request = 同一オリジンの未認証POST要求を用意する("/api/time-records/start");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await 記録一覧を取得する()).Should().BeEmpty();
    }

    [Fact]
    public async Task 未認証で記録の終了を要求すると401が返り一覧の記録は開始時のままです()
    {
        var startedRecord = await 記録を開始して終了要求の時刻まで進める(早い時刻, 遅い時刻);
        using var request = 同一オリジンの未認証POST要求を用意する($"/api/time-records/{startedRecord.Id}/end");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await 記録一覧を取得する()).Should().Equal(startedRecord);
    }

    [Fact]
    public async Task 期限切れの認証情報で記録の終了を要求すると401が返り終了時刻は未設定のままです()
    {
        var startedRecord = await 記録を開始して終了要求の時刻まで進める(早い時刻, 遅い時刻);
        using var request = 同一オリジンの未認証POST要求を用意する($"/api/time-records/{startedRecord.Id}/end");
        request.Headers.Authorization = 認証情報を用意する(expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var records = await 記録一覧を取得する();
        records.Should().ContainSingle().Which.Should().Be(startedRecord);
        records[0].EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task 別の利用者が記録の終了を要求すると404が返り所有者の一覧には元の記録が残ります()
    {
        var startedRecord = await 記録を開始して終了要求の時刻まで進める(早い時刻, 遅い時刻, "first-user");
        using var request = 同一オリジンのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end", "second-user");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await 記録一覧を取得する("first-user")).Should().Equal(startedRecord);
    }

    [Fact]
    public async Task 所有者が保存先にない記録の終了を要求すると404が返ります()
    {
        await using var factoryWithEmptyEndState = configuredFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.Replace(
                ServiceDescriptor.Singleton(new TimeRecordEndOperation(timeProvider, new InMemoryTimeRecordState())))));
        using var apiClient = factoryWithEmptyEndState.CreateClient();
        var startedRecord = await 開始済みの記録を用意する(apiClient, 早い時刻);
        timeProvider.SetUtcNow(遅い時刻);
        using var request = 同一オリジンのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end");

        using var response = await apiClient.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task 二人の利用者がそれぞれ記録を開始すると各自の一覧には本人の記録だけが含まれます()
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
    public async Task 記録の開始応答のJSONには終了時刻を表すendedAtがnullで含まれます()
    {
        using var request = 同一オリジンのPOST要求を用意する("/api/time-records/start");

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>(CancellationToken);
        json.Should().NotBeNull();
        json.Should().ContainKey("endedAt");
        json["endedAt"].Should().BeNull();
    }


    [Fact]
    public async Task 記録を終了すると200と終了した作業記録のJSONが返されます()
    {
        var startedRecord = await 記録を開始して終了要求の時刻まで進める(早い時刻, 遅い時刻);

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
    public async Task 記録の終了後に一覧を取得すると200と終了済みの記録を含むJSON配列が返ります()
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
    public async Task 認証済みでもSecFetchSiteが同一オリジンを示さない開始要求では403が返り一覧は空のままです(string? secFetchSite)
    {
        using var request = 認証済みのPOST要求を用意する("/api/time-records/start", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await 記録一覧を取得する()).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(同一オリジンを示さないヘッダー値))]
    public async Task 認証済みでもSecFetchSiteが同一オリジンを示さない終了要求では403が返り一覧の記録は開始時のままです(string? secFetchSite)
    {
        var startedRecord = await 記録を開始して終了要求の時刻まで進める(早い時刻, 遅い時刻);
        using var request = 認証済みのPOST要求を用意する($"/api/time-records/{startedRecord.Id}/end", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    static AuthenticationHeaderValue 認証情報を用意する(string user = "google-user", DateTimeOffset? expiresAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        var header = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = "endpoint-test-key" }));
        var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = "https://accounts.google.com",
            aud = "kizamu-client",
            sub = user,
            iat = now.AddHours(-2).ToUnixTimeSeconds(),
            exp = (expiresAt ?? now.AddHours(1)).ToUnixTimeSeconds()
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

    Task<TimeRecordResponse> 開始済みの記録を用意する(DateTimeOffset startedAt, string user = "google-user") =>
        開始済みの記録を用意する(client, startedAt, user);

    async Task<TimeRecordResponse> 開始済みの記録を用意する(
        HttpClient apiClient, DateTimeOffset startedAt, string user = "google-user")
    {
        timeProvider.SetUtcNow(startedAt);
        using var request = 同一オリジンのPOST要求を用意する("/api/time-records/start", user);
        using var response = await apiClient.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>(CancellationToken);
        body.Should().NotBeNull();
        return body;
    }

    async Task<TimeRecordResponse> 記録を開始して終了要求の時刻まで進める(
        DateTimeOffset startedAt, DateTimeOffset endRequestedAt, string user = "google-user")
    {
        var startedRecord = await 開始済みの記録を用意する(startedAt, user);
        timeProvider.SetUtcNow(endRequestedAt);
        return startedRecord;
    }

    async Task<Guid> 終了済みの記録を用意する(DateTimeOffset startedAt, DateTimeOffset endedAt)
    {
        var startedRecord = await 記録を開始して終了要求の時刻まで進める(startedAt, endedAt);
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
