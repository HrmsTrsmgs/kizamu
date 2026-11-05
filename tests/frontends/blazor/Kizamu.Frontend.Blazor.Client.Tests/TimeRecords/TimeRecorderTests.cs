using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecorderTests : IDisposable
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly Guid 別の記録ID = Guid.Parse("019cdeac-b600-7000-8000-000000000002");
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    static readonly DateTimeOffset 終了時刻 = DateTimeOffset.Parse("2026-08-01T10:30:00Z");
    readonly BunitContext context = new();
    readonly StubTimeRecordService service = new(new(記録ID, 開始時刻, null), new(記録ID, 開始時刻, 終了時刻));

    public TimeRecorderTests()
    {
        context.Services.AddSingleton<ITimeRecordService>(service);
        context.Services.AddTransient<TimeRecorderViewModel>();
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
    public void 読み込んだ記録の時刻と記録中の終了操作を表示します()
    {
        service.Records = [new(記録ID, 開始時刻, null), new(別の記録ID, 開始時刻, 終了時刻)];

        var tested = context.Render<TimeRecorder>();

        tested.FindAll("ul li").Should().HaveCount(2);
        tested.Find("ul li:first-child").TextContent.Should().Contain("2026/08/01 18:00").And.Contain("記録中");
        tested.Find("ul li:last-child").TextContent.Should().Contain("2026/08/01 19:30");
        tested.Find("button").TextContent.Should().Be("記録を終了");
    }

    [Fact]
    public void 開始ボタンを押すと開始時刻を日本時間で表示し終了操作に切り替えます()
    {
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").Click();

        tested.Find("time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 18:00");
        DateTimeOffset.TryParse(tested.Find("time[aria-label='開始時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(開始時刻);
        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を終了");
        tested.FindAll("ul li").Should().ContainSingle();
    }

    [Fact]
    public void 終了ボタンを押すと終了時刻と作業時間を表示し開始操作に切り替えます()
    {
        var tested = 開始する();

        tested.Find("button").Click();

        tested.Find("time[aria-label='終了時刻']").TextContent.Should().Be("2026/08/01 19:30");
        DateTimeOffset.TryParse(tested.Find("time[aria-label='終了時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(終了時刻);
        tested.Find(".recorder-panel .time-record-duration-value").TextContent.Should().Be("1時間30分");
        tested.Find("ul .time-record-duration-value").TextContent.Should().Be("1時間30分");
        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 再び開始すると操作欄の終了時刻を消します()
    {
        var tested = 開始する();
        tested.Find("button").Click();
        service.StartedRecord = new(別の記録ID, 終了時刻, null);

        tested.Find("button").Click();

        tested.FindAll("time[aria-label='終了時刻']").Should().BeEmpty();
        tested.Find("time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 19:30");
    }

    // 終了操作の表示確認に必要な画面を準備します。状態遷移の詳細はViewModelの単体テストで確認します。
    IRenderedComponent<TimeRecorder> 開始する()
    {
        var tested = context.Render<TimeRecorder>();
        tested.Find("button").Click();
        return tested;
    }

    public void Dispose() => context.Dispose();
}
