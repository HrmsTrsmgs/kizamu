using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bunit;
using Kizamu.Blazor.Client.TimeRecords;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecorderTests : IDisposable
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly Guid 別の記録ID = Guid.Parse("019cdeac-b600-7000-8000-000000000002");
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    static readonly DateTimeOffset 終了時刻 = DateTimeOffset.Parse("2026-08-01T10:30:00Z");
    readonly BunitContext context = new();
    readonly TestHttpMessageHandler handler = new();
    readonly HttpClient client;
    readonly Queue<TimeRecordResponse> 開始応答 = new([new(記録ID, 開始時刻, null)]);
    TimeRecordResponse[] 一覧応答 = [];

    public TimeRecorderTests()
    {
        client = new(handler) { BaseAddress = new("http://localhost/") };
        handler.Respond = request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = request.RequestUri?.AbsolutePath switch
            {
                "/api/time-records" => JsonContent.Create(一覧応答),
                "/api/time-records/start" => JsonContent.Create(開始応答.Dequeue()),
                _ => JsonContent.Create(new TimeRecordResponse(記録ID, 開始時刻, 終了時刻))
            }
        });
        context.Services.AddSingleton(new TimeRecordApi(client));
        context.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(終了時刻));
    }

    [Fact]
    public void 初期表示では開始操作と空の一覧を表示します()
    {
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").TextContent.Should().Be("記録を開始");
        tested.Find("ul[aria-label='作業記録一覧']").Children.Should().BeEmpty();
    }

    [Fact]
    public void 画面を表示すると作業記録一覧を要求します()
    {
        context.Render<TimeRecorder>();

        handler.Requests.Should().Equal((HttpMethod.Get, "/api/time-records"));
    }

    [Fact]
    public void 取得した複数の記録を開始時刻と終了時刻付きで一覧に表示します()
    {
        一覧応答 = [new(記録ID, 開始時刻, null), new(別の記録ID, 開始時刻, 終了時刻)];

        var tested = context.Render<TimeRecorder>();

        tested.FindAll("ul li").Should().HaveCount(2);
        tested.Find("ul li:first-child").TextContent.Should().Contain("2026/08/01 18:00").And.Contain("記録中");
        tested.Find("ul li:last-child").TextContent.Should().Contain("2026/08/01 19:30");
    }

    [Fact]
    public void 終了済みの記録だけを取得した場合は開始操作を表示します()
    {
        一覧応答 = [new(記録ID, 開始時刻, 終了時刻)];

        var tested = context.Render<TimeRecorder>();

        tested.Find("button").TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 取得した記録中の作業を終了できます()
    {
        一覧応答 = [new(記録ID, 開始時刻, null)];
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").TextContent.Should().Be("記録を終了");
        tested.Find("button").Click();

        handler.Requests.Should().Contain((HttpMethod.Post, $"/api/time-records/{記録ID}/end"));
    }

    [Fact]
    public void 開始ボタンを押すと記録開始を要求します()
    {
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").Click();

        handler.Requests.Should().Equal((HttpMethod.Get, "/api/time-records"), (HttpMethod.Post, "/api/time-records/start"));
    }

    [Fact]
    public void 記録を開始すると開始時刻を日本時間で表示します()
    {
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").Click();

        tested.Find("time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 18:00");
        DateTimeOffset.TryParse(tested.Find("time[aria-label='開始時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(開始時刻);
    }

    [Fact]
    public void 記録を開始すると既存の一覧へ追加します()
    {
        一覧応答 = [new(別の記録ID, 開始時刻, 終了時刻)];
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").Click();

        tested.FindAll("ul li").Should().HaveCount(2);
        tested.Find("ul li:last-child").TextContent.Should().Contain("2026/08/01 18:00");
    }

    [Fact]
    public void 記録を開始すると終了操作へ切り替わります()
    {
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").Click();

        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を終了");
    }

    [Fact]
    public void 終了ボタンを押すと開始した記録の終了を要求します()
    {
        var tested = 開始する();

        tested.Find("button").Click();

        handler.Requests.Last().Should().Be((HttpMethod.Post, $"/api/time-records/{記録ID}/end"));
    }

    [Fact]
    public void 記録を終了すると終了時刻を日本時間で表示します()
    {
        var tested = 開始する();

        tested.Find("button").Click();

        tested.Find("time[aria-label='終了時刻']").TextContent.Should().Be("2026/08/01 19:30");
        DateTimeOffset.TryParse(tested.Find("time[aria-label='終了時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(終了時刻);
    }

    [Fact]
    public void 記録を終了すると操作欄と一覧に同じ作業時間を表示します()
    {
        var tested = 開始する();

        tested.Find("button").Click();

        tested.Find(".recorder-panel .time-record-duration-value").TextContent.Should().Be("1時間30分");
        tested.Find("ul .time-record-duration-value").TextContent.Should().Be("1時間30分");
    }

    [Fact]
    public void 記録を終了すると対象の一覧項目だけを終了済みに更新します()
    {
        一覧応答 = [new(別の記録ID, 開始時刻, 終了時刻)];
        var tested = 開始する();

        tested.Find("button").Click();

        tested.FindAll("ul li").Should().HaveCount(2);
        tested.FindAll("ul time").Should().HaveCount(4);
        tested.Find("ul li:last-child").TextContent.Should().Contain("2026/08/01 19:30");
    }

    [Fact]
    public void 記録を終了すると開始操作へ切り替わります()
    {
        var tested = 開始する();

        tested.Find("button").Click();

        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 再び開始すると前回の終了時刻を操作欄から消して履歴は残します()
    {
        開始応答.Enqueue(new(別の記録ID, 終了時刻, null));
        var tested = 開始する();
        tested.Find("button").Click();

        tested.Find("button").Click();

        tested.FindAll("time[aria-label='終了時刻']").Should().BeEmpty();
        tested.FindAll("ul li").Should().HaveCount(2);
        tested.Find("ul li:first-child").TextContent.Should().Contain("2026/08/01 19:30");
    }

    // 終了操作のテストに共通する準備です。開始自体の仕様は別のテストで確認します。
    IRenderedComponent<TimeRecorder> 開始する()
    {
        var tested = context.Render<TimeRecorder>();
        tested.Find("button").Click();
        return tested;
    }

    public void Dispose()
    {
        context.Dispose();
        client.Dispose();
    }
}
