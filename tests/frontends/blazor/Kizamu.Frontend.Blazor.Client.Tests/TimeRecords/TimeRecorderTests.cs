using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecorderTests : IDisposable
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly Guid 別の記録ID = Guid.Parse("019cdeac-b600-7000-8000-000000000002");
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    static readonly DateTimeOffset 終了時刻 = DateTimeOffset.Parse("2026-08-01T10:30:00Z");
    readonly BunitContext context = new();
    readonly Mock<ITimeRecorderViewModel> recorder = new();

    public TimeRecorderTests()
    {
        記録一覧を用意する([]);
        context.Services.AddSingleton(recorder.Object);
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
        記録一覧を用意する([new(記録ID, 開始時刻, null), new(別の記録ID, 開始時刻, 終了時刻)]);
        記録中の状態を用意する(true);

        var tested = context.Render<TimeRecorder>();

        tested.FindAll("ul li").Should().HaveCount(2);
        tested.Find("ul li:first-child").TextContent.Should().Contain("2026/08/01 18:00").And.Contain("記録中");
        tested.Find("ul li:last-child").TextContent.Should().Contain("2026/08/01 19:30");
        tested.Find("button").TextContent.Should().Be("記録を終了");
    }

    [Fact]
    public void 開始ボタンを押すと開始操作を呼びます()
    {
        var tested = context.Render<TimeRecorder>();
        tested.Find("button").Click();
        開始操作の呼び出しを確認する(Times.Once());
        終了操作の呼び出しを確認する(Times.Never());
    }

    [Fact]
    public async Task 開始操作に失敗すると利用者に知らせます()
    {
        開始操作の失敗を用意する(new HttpRequestException());
        var tested = context.Render<TimeRecorder>();

        await tested.Find("button").ClickAsync(new());

        tested.Find("[role='alert']").TextContent.Should().Contain("失敗");
    }

    [Fact]
    public void 開始済みの状態に開始時刻と終了操作を表示します()
    {
        記録中の状態を用意する(true);
        TimeRecord 記録 = new(記録ID, 開始時刻, null);
        操作欄の記録を用意する(記録);
        記録一覧を用意する([記録]);
        var tested = context.Render<TimeRecorder>();

        tested.Find("time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 18:00");
        DateTimeOffset.TryParse(tested.Find("time[aria-label='開始時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(開始時刻);
        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を終了");
        tested.FindAll("ul li").Should().ContainSingle();
    }

    [Fact]
    public void 終了ボタンを押すと終了操作を呼びます()
    {
        記録中の状態を用意する(true);
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").Click();

        終了操作の呼び出しを確認する(Times.Once());
        開始操作の呼び出しを確認する(Times.Never());
    }

    [Fact]
    public void 終了済みの状態に終了時刻と作業時間と開始操作を表示します()
    {
        TimeRecord 記録 = new(記録ID, 開始時刻, 終了時刻);
        操作欄の記録を用意する(記録);
        記録一覧を用意する([記録]);
        var tested = context.Render<TimeRecorder>();

        tested.Find("time[aria-label='終了時刻']").TextContent.Should().Be("2026/08/01 19:30");
        DateTimeOffset.TryParse(tested.Find("time[aria-label='終了時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(終了時刻);
        tested.Find(".recorder-panel .time-record-duration-value").TextContent.Should().Be("1時間30分");
        tested.Find("ul .time-record-duration-value").TextContent.Should().Be("1時間30分");
        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 再開始済みの状態には終了時刻を表示しません()
    {
        記録中の状態を用意する(true);
        操作欄の記録を用意する(new(別の記録ID, 終了時刻, null));
        var tested = context.Render<TimeRecorder>();
        tested.FindAll("time[aria-label='終了時刻']").Should().BeEmpty();
        tested.Find("time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 19:30");
    }

    [Fact]
    public void 通信中はボタンを無効にし完了後に有効にします()
    {
        通信中の状態を用意する(true);
        var tested = context.Render<TimeRecorder>();
        tested.Find("button").HasAttribute("disabled").Should().BeTrue();
        通信中の状態を用意する(false);
        tested.Render();
        tested.Find("button").HasAttribute("disabled").Should().BeFalse();
    }

    [Theory]
    [InlineData(false, "記録を開始")]
    [InlineData(true, "記録を終了")]
    public void 最新状態を確認できないときは記録操作を無効にして再取得を案内します(bool 記録中, string 操作名)
    {
        記録中の状態を用意する(記録中);
        未確認の状態を用意する(true);

        var tested = context.Render<TimeRecorder>();

        tested.Find("[role='alert']").TextContent.Should().Contain("最新状態を確認できません");
        tested.FindAll("button").Single(it => it.TextContent == 操作名).HasAttribute("disabled").Should().BeTrue();
        tested.FindAll("button").Single(it => it.TextContent == "再取得").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task 再取得ボタンを押すと一覧読み込みを呼びます()
    {
        未確認の状態を用意する(true);
        var tested = context.Render<TimeRecorder>();

        await tested.FindAll("button").Single(it => it.TextContent == "再取得").ClickAsync(new());

        一覧読み込みの呼び出しを確認する(Times.Exactly(2));
        開始操作の呼び出しを確認する(Times.Never());
        終了操作の呼び出しを確認する(Times.Never());
    }

    [Fact]
    public void 再取得の通信中は再取得ボタンを無効にします()
    {
        未確認の状態を用意する(true);
        通信中の状態を用意する(true);

        var tested = context.Render<TimeRecorder>();

        tested.FindAll("button").Single(it => it.TextContent == "再取得").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task 再取得に失敗しても未確認の案内と再取得操作を表示します()
    {
        未確認の状態を用意する(true);
        再取得の失敗を用意する(new HttpRequestException());
        var tested = context.Render<TimeRecorder>();

        await tested.FindAll("button").Single(it => it.TextContent == "再取得").ClickAsync(new());

        tested.Find("[role='alert']").TextContent.Should().Contain("最新状態を確認できません");
        tested.FindAll("button").Single(it => it.TextContent == "再取得").HasAttribute("disabled").Should().BeFalse();
    }

    void 未確認の状態を用意する(bool isStateUnconfirmed) => recorder.SetupGet(it => it.IsStateUnconfirmed).Returns(isStateUnconfirmed);
    void 一覧読み込みの呼び出しを確認する(Times times) => recorder.Verify(it => it.LoadAsync(), times);
    void 再取得の失敗を用意する(Exception error) =>
        recorder.SetupSequence(it => it.LoadAsync()).Returns(Task.CompletedTask).ThrowsAsync(error);

    void 開始操作の失敗を用意する(Exception error) => recorder.Setup(it => it.StartTimeRecordAsync()).ThrowsAsync(error);
    void 記録一覧を用意する(IReadOnlyList<TimeRecord> records) => recorder.SetupGet(it => it.Records).Returns(records);
    void 記録中の状態を用意する(bool isRecording) => recorder.SetupGet(it => it.IsRecording).Returns(isRecording);
    void 操作欄の記録を用意する(TimeRecord record) => recorder.SetupGet(it => it.DisplayedRecord).Returns(record);
    void 通信中の状態を用意する(bool isBusy) => recorder.SetupGet(it => it.IsBusy).Returns(isBusy);
    void 開始操作の呼び出しを確認する(Times times) => recorder.Verify(it => it.StartTimeRecordAsync(), times);
    void 終了操作の呼び出しを確認する(Times times) => recorder.Verify(it => it.EndTimeRecordAsync(), times);

    public void Dispose() => context.Dispose();
}
