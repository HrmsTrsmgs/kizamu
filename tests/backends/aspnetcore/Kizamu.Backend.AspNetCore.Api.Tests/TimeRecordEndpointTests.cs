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

    readonly FakeTimeProvider timeProvider;
    readonly WebApplicationFactory<Program> configuredFactory;
    readonly HttpClient client;

    public TimeRecordEndpointTests(WebApplicationFactory<Program> factory)
    {
        timeProvider = new FakeTimeProvider();
        configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton<TimeProvider>(timeProvider)));
        client = configuredFactory.CreateClient();
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
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        using var endResponse = await 記録の終了応答を取得する(startedRecord.Id, 遅い時刻);
        endResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.GetAsync("/api/time-records", CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse[]>(CancellationToken);
        body.Should().Equal(
            new TimeRecordResponse(startedRecord.Id, 早い時刻, 遅い時刻));
    }

    [Theory]
    [InlineData("same-site")]
    [InlineData("cross-site")]
    [InlineData("none")]
    [InlineData("unknown")]
    [InlineData(null)]
    public async Task SecFetchSiteが同一オリジンを示さない開始要求は403になり記録が追加されません(string? secFetchSite)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/time-records/start");
        if (secFetchSite is not null) request.Headers.Add("Sec-Fetch-Site", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var records = await 記録一覧を取得する();
        records.Should().BeEmpty();
    }

    [Theory]
    [InlineData("same-site")]
    [InlineData("cross-site")]
    [InlineData("none")]
    [InlineData("unknown")]
    [InlineData(null)]
    public async Task SecFetchSiteが同一オリジンを示さない終了要求は403になり記録が終了されません(string? secFetchSite)
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/time-records/{startedRecord.Id}/end");
        if (secFetchSite is not null) request.Headers.Add("Sec-Fetch-Site", secFetchSite);

        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var records = await 記録一覧を取得する();
        records.Should().Equal(startedRecord);
    }

    async Task<TimeRecordResponse[]?> 記録一覧を取得する()
    {
        using var response = await client.GetAsync("/api/time-records", CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<TimeRecordResponse[]>(CancellationToken);
    }

    static HttpRequestMessage 同一オリジンのPOST要求を用意する(string requestUri)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        return request;
    }

    async Task<TimeRecordResponse> 開始済みの記録を用意する(DateTimeOffset startedAt)
    {
        timeProvider.SetUtcNow(startedAt);
        using var request = 同一オリジンのPOST要求を用意する("/api/time-records/start");
        using var response = await client.SendAsync(request, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>(CancellationToken);
        body.Should().NotBeNull();
        return body;
    }

    Task<HttpResponseMessage> 記録の終了応答を取得する(Guid id, DateTimeOffset endedAt)
    {
        timeProvider.SetUtcNow(endedAt);
        return 終了要求を送る(id);
    }

    async Task<HttpResponseMessage> 終了要求を送る(Guid id)
    {
        using var request = 同一オリジンのPOST要求を用意する($"/api/time-records/{id}/end");
        return await client.SendAsync(request, CancellationToken);
    }

    public void Dispose()
    {
        client.Dispose();
        configuredFactory.Dispose();
    }

    sealed record TimeRecordResponse(
        Guid Id,
        DateTimeOffset StartedAt,
        DateTimeOffset? EndedAt);
}
