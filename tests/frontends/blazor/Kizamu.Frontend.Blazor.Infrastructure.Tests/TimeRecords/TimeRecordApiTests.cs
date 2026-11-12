using System.Collections.Immutable;
using System.Net;
using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;

namespace Kizamu.Frontend.Blazor.Infrastructure.Tests.TimeRecords;

public sealed class TimeRecordApiTests : IDisposable
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly Guid 別の記録ID = Guid.Parse("019cdeac-b600-7000-8000-000000000002");
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    static readonly DateTimeOffset 終了時刻 = DateTimeOffset.Parse("2026-08-01T10:30:00Z");
    readonly TestHttpMessageHandler handler = new();
    readonly HttpClient client;
    readonly TimeRecordApi tested;

    public TimeRecordApiTests()
    {
        client = new(handler) { BaseAddress = new("http://localhost/") };
        tested = new(client);
    }

    [Fact]
    public async Task 作業記録一覧APIへGETします()
    {
        await tested.GetTimeRecordsAsync();

        handler.Requests.Should().Equal((HttpMethod.Get, "/api/time-records"));
    }

    [Fact]
    public async Task 作業記録一覧APIの応答を作業記録へ変換します()
    {
        TimeRecordResponse[] 記録 = [new(記録ID, 開始時刻, null), new(別の記録ID, 開始時刻, 終了時刻)];
        handler.RespondWithJson(記録);

        ImmutableArray<TimeRecord> 受信した記録 = await tested.GetTimeRecordsAsync();

        受信した記録.Should().BeEquivalentTo(記録, it => it.WithStrictOrdering());
    }

    [Fact]
    public async Task 記録開始APIへPOSTします()
    {
        handler.RespondWithJson(new TimeRecordResponse(記録ID, 開始時刻, null));

        await tested.StartTimeRecordAsync();

        handler.Requests.Should().Equal((HttpMethod.Post, "/api/time-records/start"));
    }

    [Fact]
    public async Task 記録開始APIの応答を作業記録へ変換します()
    {
        TimeRecordResponse 記録 = new(記録ID, 開始時刻, null);
        handler.RespondWithJson(記録);

        TimeRecord 受信した記録 = await tested.StartTimeRecordAsync();

        受信した記録.Should().BeEquivalentTo(記録);
    }

    [Fact]
    public async Task 指定した記録の終了APIへPOSTします()
    {
        handler.RespondWithJson(new TimeRecordResponse(記録ID, 開始時刻, 終了時刻));

        await tested.EndTimeRecordAsync(記録ID);

        handler.Requests.Should().Equal((HttpMethod.Post, $"/api/time-records/{記録ID}/end"));
    }

    [Fact]
    public async Task 記録終了APIの応答を作業記録へ変換します()
    {
        TimeRecordResponse 記録 = new(記録ID, 開始時刻, 終了時刻);
        handler.RespondWithJson(記録);

        TimeRecord 受信した記録 = await tested.EndTimeRecordAsync(記録ID);

        受信した記録.Should().BeEquivalentTo(記録);
    }

    [Fact]
    public async Task 作業記録一覧APIがHTTPエラーを返したら本文が正常でも失敗を通知します()
    {
        handler.RespondWithJson<TimeRecordResponse[]>([new(記録ID, 開始時刻, null)], HttpStatusCode.InternalServerError);

        var 操作 = () => tested.GetTimeRecordsAsync();

        (await 操作.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task 記録開始APIがHTTPエラーを返したら本文が正常でも失敗を通知します()
    {
        handler.RespondWithJson(new TimeRecordResponse(記録ID, 開始時刻, null), HttpStatusCode.InternalServerError);

        var 操作 = () => tested.StartTimeRecordAsync();

        (await 操作.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task 指定した記録の終了APIがHTTPエラーを返したら本文が正常でも失敗を通知します()
    {
        handler.RespondWithJson(new TimeRecordResponse(記録ID, 開始時刻, 終了時刻), HttpStatusCode.InternalServerError);

        var 操作 = () => tested.EndTimeRecordAsync(記録ID);

        (await 操作.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    public void Dispose() => client.Dispose();
}
