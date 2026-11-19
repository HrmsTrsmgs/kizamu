using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
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
    readonly TimeRecordResponse 開始した記録 = new(記録ID, 開始時刻, null);
    readonly TimeRecordResponse 終了した記録 = new(記録ID, 開始時刻, 終了時刻);
    readonly TestHttpMessageHandler handler = new();
    readonly HttpClient client;
    readonly TimeRecordApi tested;

    public TimeRecordApiTests()
    {
        client = new(handler) { BaseAddress = new("http://localhost/") };
        tested = new(client);
    }

    [Fact]
    public async Task 作業記録一覧の取得には一覧APIへGET要求を送ります()
    {
        await tested.GetTimeRecordsAsync();

        handler.Requests.Should().Equal((HttpMethod.Get, "/api/time-records"));
    }

    [Fact]
    public async Task 記録一覧を取得するとAPIの応答に並んだ順に作業記録が返ります()
    {
        TimeRecordResponse[] 記録 = [開始した記録, new(別の記録ID, 開始時刻, 終了時刻)];
        handler.RespondWithJson(記録);

        ImmutableArray<TimeRecord> 受信した記録 = await tested.GetTimeRecordsAsync();

        受信した記録.Should().BeEquivalentTo(記録, it => it.WithStrictOrdering());
    }

    [Fact]
    public async Task 作業記録の開始には記録開始APIへPOST要求を送ります()
    {
        handler.RespondWithJson(開始した記録);

        await tested.StartTimeRecordAsync();

        handler.Requests.Should().Equal((HttpMethod.Post, "/api/time-records/start"));
    }

    [Fact]
    public async Task 記録開始APIの応答を作業記録へ変換します()
    {
        handler.RespondWithJson(開始した記録);

        TimeRecord 受信した記録 = await tested.StartTimeRecordAsync();

        受信した記録.Should().BeEquivalentTo(開始した記録);
    }

    [Fact]
    public async Task 記録開始APIの成功応答に記録IDがないとJsonExceptionが発生します()
    {
        handler.RespondWithJson(new { startedAt = 開始時刻, endedAt = (DateTimeOffset?)null }, HttpStatusCode.Created);

        var 操作 = () => tested.StartTimeRecordAsync();

        await 操作.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task 作業記録の終了には指定した記録IDの終了APIへPOST要求を送ります()
    {
        handler.RespondWithJson(終了した記録);

        await tested.EndTimeRecordAsync(記録ID);

        handler.Requests.Should().Equal((HttpMethod.Post, $"/api/time-records/{記録ID}/end"));
    }

    [Fact]
    public async Task 記録終了APIの応答を作業記録へ変換します()
    {
        handler.RespondWithJson(終了した記録);

        TimeRecord 受信した記録 = await tested.EndTimeRecordAsync(記録ID);

        受信した記録.Should().BeEquivalentTo(終了した記録);
    }

    [Fact]
    public async Task 作業記録一覧APIの応答がHTTPエラーなら本文が正常でもステータスコード付きの例外が発生します()
    {
        handler.RespondWithJson<TimeRecordResponse[]>([開始した記録], HttpStatusCode.InternalServerError);

        var 操作 = () => tested.GetTimeRecordsAsync();

        (await 操作.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task 記録開始APIの応答がHTTPエラーなら本文が正常でもステータスコード付きの例外が発生します()
    {
        handler.RespondWithJson(開始した記録, HttpStatusCode.InternalServerError);

        var 操作 = () => tested.StartTimeRecordAsync();

        (await 操作.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task 記録終了APIの応答がHTTPエラーなら本文が正常でもステータスコード付きの例外が発生します()
    {
        handler.RespondWithJson(終了した記録, HttpStatusCode.InternalServerError);

        var 操作 = () => tested.EndTimeRecordAsync(記録ID);

        (await 操作.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    public void Dispose() => client.Dispose();
}
