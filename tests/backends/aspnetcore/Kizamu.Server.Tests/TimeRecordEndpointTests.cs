using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Server.Tests;

public sealed class TimeRecordEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    static readonly DateTimeOffset 早い時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 中間の時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
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

    public static TheoryData<DateTimeOffset> 開始時刻の例 => [早い時刻, 中間の時刻];

    public static TheoryData<DateTimeOffset, DateTimeOffset> 記録時刻の例 => new()
    {
        { 早い時刻, 中間の時刻 },
        { 中間の時刻, 遅い時刻 },
    };

    [Theory]
    [MemberData(nameof(開始時刻の例))]
    public async Task 記録を開始すると識別子と開始時刻があり終了時刻がない作業記録が返されます(
        DateTimeOffset startedAt)
    {
        timeProvider.SetUtcNow(startedAt);

        using var response = await client.PostAsync("/api/time-records/start", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>();
        body.Should().NotBeNull();
        body.Id.Should().NotBe(Guid.Empty);
        body.StartedAt.Should().Be(startedAt);
        body.EndedAt.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(記録時刻の例))]
    public async Task 記録を終了すると識別子と開始時刻と終了時刻が返されます(
        DateTimeOffset startedAt,
        DateTimeOffset endedAt)
    {
        var startedRecord = await 開始済みの記録を用意する(startedAt);
        timeProvider.SetUtcNow(endedAt);

        using var response = await client.PostAsync($"/api/time-records/{startedRecord.Id}/end", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>();
        body.Should().NotBeNull();
        body.Id.Should().Be(startedRecord.Id);
        body.StartedAt.Should().Be(startedAt);
        body.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public async Task 記録を開始した後に一覧を取得すると開始した記録が含まれます()
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);

        using var response = await client.GetAsync("/api/time-records");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse[]>();
        body.Should().ContainEquivalentOf(startedRecord);
    }

    [Fact]
    public async Task 二件の記録を開始した後に一覧を取得すると開始した順に両方が返されます()
    {
        var firstRecord = await 開始済みの記録を用意する(早い時刻);
        var secondRecord = await 開始済みの記録を用意する(遅い時刻);

        using var response = await client.GetAsync("/api/time-records");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse[]>();
        body.Should().Equal(firstRecord, secondRecord);
    }

    [Fact]
    public async Task 記録を終了した後に一覧を取得すると終了時刻が含まれます()
    {
        var startedRecord = await 開始済みの記録を用意する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);
        using var endResponse = await client.PostAsync($"/api/time-records/{startedRecord.Id}/end", null);
        endResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.GetAsync("/api/time-records");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse[]>();
        body.Should().ContainEquivalentOf(
            new TimeRecordResponse(startedRecord.Id, 早い時刻, 遅い時刻));
    }

    async Task<TimeRecordResponse> 開始済みの記録を用意する(DateTimeOffset startedAt)
    {
        // 開始APIの仕様を直接読めるよう、開始テストと同じHTTP処理を意図的に共通化していない。
        timeProvider.SetUtcNow(startedAt);
        using var response = await client.PostAsync("/api/time-records/start", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TimeRecordResponse>();
        body.Should().NotBeNull();
        return body;
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
