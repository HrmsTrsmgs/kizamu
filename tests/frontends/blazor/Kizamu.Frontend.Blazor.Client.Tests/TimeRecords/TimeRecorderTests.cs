using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;
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
        記録操作の可否を用意する(true);
        利用者の操作を記録処理へつなぐ();
        context.Services.AddSingleton(recorder.Object);
        context.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(終了時刻));
    }

    [Fact]
    public void 初期表示では開始操作を表示します()
    {
        var tested = context.Render<TimeRecorder>();

        tested.Find("button").TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 初期表示では作業記録一覧が空です()
    {
        var tested = context.Render<TimeRecorder>();

        tested.Find("ul[aria-label='作業記録一覧']").Children.Should().BeEmpty();
    }

    [Fact]
    public void 読み込んだ記録の時刻を一覧の順に表示します()
    {
        var tested = 記録中と終了済みの一覧を表示する();

        tested.FindAll("ul li").Should().HaveCount(2);
        tested.Find("ul li:first-child").TextContent.Should().Contain("2026/08/01 18:00").And.Contain("記録中");
        tested.Find("ul li:last-child").TextContent.Should().Contain("2026/08/01 19:30");
    }

    [Fact]
    public void 読み込んだ記録が記録中なら終了操作を表示します()
    {
        var tested = 記録中と終了済みの一覧を表示する();

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
        開始に失敗する記録処理を用意する();
        var tested = context.Render<TimeRecorder>();

        await tested.Find("button").ClickAsync(new());

        tested.Find("[role='alert']").TextContent.Should().Contain("失敗");
    }

    [Fact]
    public void 開始後の操作欄に開始時刻を表示します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, null));

        tested.Find("time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 18:00");
    }

    [Fact]
    public void 開始後の操作欄には開始時刻を示すdatetime属性を設定します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, null));

        DateTimeOffset.TryParse(tested.Find("time[aria-label='開始時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(開始時刻);
    }

    [Fact]
    public void 開始後は終了ボタンだけを表示します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, null));

        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を終了");
    }

    [Fact]
    public void 開始後の記録を一覧に表示します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, null));

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
    public void 終了後の操作欄に終了時刻を表示します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, 終了時刻));

        tested.Find("time[aria-label='終了時刻']").TextContent.Should().Be("2026/08/01 19:30");
    }

    [Fact]
    public void 終了後の操作欄には終了時刻を示すdatetime属性を設定します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, 終了時刻));

        DateTimeOffset.TryParse(tested.Find("time[aria-label='終了時刻']").GetAttribute("datetime"), out var timestamp).Should().BeTrue();
        timestamp.Should().Be(終了時刻);
    }

    [Fact]
    public void 終了後の操作欄に作業時間を表示します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, 終了時刻));

        tested.Find(".recorder-panel .time-record-duration-value").TextContent.Should().Be("1時間30分");
    }

    [Fact]
    public void 終了後の記録一覧に作業時間を表示します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, 終了時刻));

        tested.Find("ul .time-record-duration-value").TextContent.Should().Be("1時間30分");
    }

    [Fact]
    public void 終了後は開始ボタンだけを表示します()
    {
        var tested = 操作結果のある画面を表示する(new(記録ID, 開始時刻, 終了時刻));

        tested.FindAll("button").Should().ContainSingle().Which.TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 再開始済みの状態には終了時刻を表示しません()
    {
        記録中の状態を用意する(true);
        操作欄の記録を用意する(new(別の記録ID, 終了時刻, null));
        var tested = context.Render<TimeRecorder>();

        tested.FindAll("time[aria-label='終了時刻']").Should().BeEmpty();
    }

    [Fact]
    public void 再開始済みの状態には新しい開始時刻を表示します()
    {
        記録中の状態を用意する(true);
        操作欄の記録を用意する(new(別の記録ID, 終了時刻, null));
        var tested = context.Render<TimeRecorder>();

        tested.Find("time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 19:30");
    }

    [Fact]
    public void 記録操作の可否をボタンの無効状態に反映します()
    {
        記録操作の可否を用意する(false);
        var tested = context.Render<TimeRecorder>();
        tested.Find("button").HasAttribute("disabled").Should().BeTrue();
        記録操作の可否を用意する(true);
        tested.Render();
        tested.Find("button").HasAttribute("disabled").Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 最新状態を確認できないときは警告を表示します(bool 記録中)
    {
        var tested = 最新状態を確認できない画面を表示する(記録中);

        tested.Find("[role='alert']").TextContent.Should().Contain("最新状態を確認できません");
    }

    [Theory]
    [InlineData(false, "記録を開始")]
    [InlineData(true, "記録を終了")]
    public void 最新状態を確認できないときは記録操作を無効にします(bool 記録中, string 操作名)
    {
        var tested = 最新状態を確認できない画面を表示する(記録中);

        tested.FindAll("button").Single(it => it.TextContent == 操作名).HasAttribute("disabled").Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void 最新状態を確認できないときは再取得を選べます(bool 記録中)
    {
        var tested = 最新状態を確認できない画面を表示する(記録中);

        tested.FindAll("button").Single(it => it.TextContent == "再取得").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task 再取得ボタンを押すと一覧読み込みを呼びます()
    {
        再取得の案内を用意する();
        var tested = context.Render<TimeRecorder>();

        await tested.FindAll("button").Single(it => it.TextContent == "再取得").ClickAsync(new());

        一覧読み込みの呼び出しを確認する(Times.Exactly(2));
        開始操作の呼び出しを確認する(Times.Never());
        終了操作の呼び出しを確認する(Times.Never());
    }

    [Fact]
    public async Task 再取得ボタンを押すと利用者の操作として実行します()
    {
        再取得の案内を用意する();
        var tested = context.Render<TimeRecorder>();

        await tested.FindAll("button").Single(it => it.TextContent == "再取得").ClickAsync(new());

        利用者の操作の呼び出しを確認する(Times.Once());
    }

    [Fact]
    public void 再取得の通信中は再取得ボタンを無効にします()
    {
        再取得の案内を用意する();
        通信中の状態を用意する(true);

        var tested = context.Render<TimeRecorder>();

        tested.FindAll("button").Single(it => it.TextContent == "再取得").HasAttribute("disabled").Should().BeTrue();
    }

    IRenderedComponent<TimeRecorder> 記録中と終了済みの一覧を表示する()
    {
        記録一覧を用意する([new(記録ID, 開始時刻, null), new(別の記録ID, 開始時刻, 終了時刻)]);
        記録中の状態を用意する(true);
        return context.Render<TimeRecorder>();
    }

    IRenderedComponent<TimeRecorder> 操作結果のある画面を表示する(TimeRecord 記録)
    {
        記録中の状態を用意する(記録.EndedAt is null);
        操作欄の記録を用意する(記録);
        記録一覧を用意する([記録]);
        return context.Render<TimeRecorder>();
    }

    IRenderedComponent<TimeRecorder> 最新状態を確認できない画面を表示する(bool 記録中)
    {
        記録中の状態を用意する(記録中);
        再取得の案内を用意する();
        記録操作の可否を用意する(false);
        return context.Render<TimeRecorder>();
    }

    void 開始に失敗する記録処理を用意する()
    {
        context.Services.AddSingleton<ITimeRecorderViewModel>(
            new TimeRecorderViewModel(
                new StubTimeRecordService(new(記録ID, 開始時刻, null), new(記録ID, 開始時刻, 終了時刻))
                {
                    StartResponse = Task.FromException<TimeRecord>(new InvalidOperationException())
                }));
    }

    void 利用者の操作の呼び出しを確認する(Times times) =>
        recorder.Verify(it => it.RunUserOperationAsync(It.IsAny<Func<Task>>()), times);
    void 再取得の案内を用意する() => recorder.SetupGet(it => it.Notice).Returns(TimeRecorderNotice.StateUnconfirmed);
    void 一覧読み込みの呼び出しを確認する(Times times) => recorder.Verify(it => it.LoadAsync(), times);
    void 利用者の操作を記録処理へつなぐ() =>
        recorder.Setup(it => it.RunUserOperationAsync(It.IsAny<Func<Task>>())).Returns((Func<Task> operation) => operation());
    void 記録操作の可否を用意する(bool canOperate) => recorder.SetupGet(it => it.CanOperate).Returns(canOperate);
    void 記録一覧を用意する(IReadOnlyList<TimeRecord> records) => recorder.SetupGet(it => it.Records).Returns(records);
    void 記録中の状態を用意する(bool isRecording) => recorder.SetupGet(it => it.IsRecording).Returns(isRecording);
    void 操作欄の記録を用意する(TimeRecord record) => recorder.SetupGet(it => it.DisplayedRecord).Returns(record);
    void 通信中の状態を用意する(bool isBusy) => recorder.SetupGet(it => it.IsBusy).Returns(isBusy);
    void 開始操作の呼び出しを確認する(Times times) => recorder.Verify(it => it.StartTimeRecordAsync(), times);
    void 終了操作の呼び出しを確認する(Times times) => recorder.Verify(it => it.EndTimeRecordAsync(), times);

    public void Dispose() => context.Dispose();
}
