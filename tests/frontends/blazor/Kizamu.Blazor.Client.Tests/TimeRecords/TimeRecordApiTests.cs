using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Kizamu.Blazor.Client.TimeRecords;

namespace Kizamu.Blazor.Client.Tests.TimeRecords;

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
    public async Task 作業記録一覧APIの応答を返します()
    {
        TimeRecordResponse[] 記録 = [new(記録ID, 開始時刻, null), new(別の記録ID, 開始時刻, 終了時刻)];
        応答する(記録);

        (await tested.GetTimeRecordsAsync()).Should().Equal(記録);
    }

    [Fact]
    public async Task 記録開始APIへPOSTします()
    {
        応答する(new TimeRecordResponse(記録ID, 開始時刻, null));

        await tested.StartTimeRecordAsync();

        handler.Requests.Should().Equal((HttpMethod.Post, "/api/time-records/start"));
    }

    [Fact]
    public async Task 記録開始APIの応答を返します()
    {
        TimeRecordResponse 記録 = new(記録ID, 開始時刻, null);
        応答する(記録);

        (await tested.StartTimeRecordAsync()).Should().Be(記録);
    }

    [Fact]
    public async Task 指定した記録の終了APIへPOSTします()
    {
        応答する(new TimeRecordResponse(記録ID, 開始時刻, 終了時刻));

        await tested.EndTimeRecordAsync(記録ID);

        handler.Requests.Should().Equal((HttpMethod.Post, $"/api/time-records/{記録ID}/end"));
    }

    [Fact]
    public async Task 記録終了APIの応答を返します()
    {
        TimeRecordResponse 記録 = new(記録ID, 開始時刻, 終了時刻);
        応答する(記録);

        (await tested.EndTimeRecordAsync(記録ID)).Should().Be(記録);
    }

    void 応答する<T>(T body) => handler.Respond = it =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });

    public void Dispose() => client.Dispose();
}
