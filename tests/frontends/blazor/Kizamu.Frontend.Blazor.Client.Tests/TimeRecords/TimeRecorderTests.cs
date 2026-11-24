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
    readonly TimeRecord 開始した記録 = new(記録ID, 開始時刻, null);
    readonly TimeRecord 終了した記録 = new(記録ID, 開始時刻, 終了時刻);
    readonly BunitContext context = new();
    readonly Mock<ITimeRecorderViewModel> recorder = new();

    IRenderedComponent<TimeRecorder> 画面 => field ??= 記録画面を表示する(recorder.Object);
    IRenderedComponent<TimeRecorder> 開始後の画面 => field ??= 操作結果のある画面を表示する(開始した記録);
    IRenderedComponent<TimeRecorder> 終了後の画面 => field ??= 操作結果のある画面を表示する(終了した記録);

    public TimeRecorderTests()
    {
        記録一覧を用意する([]);
        記録操作の可否を用意する(true);
        再取得の可否を用意する(true);
        context.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(終了時刻));
    }

    [Fact]
    public void 記録画面の初期表示では記録の開始ボタンを表示します()
    {
        画面.Find("button").TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 明示したViewModelだけで記録画面を表示してその一覧を取得します()
    {
        using var standalone = new BunitContext();

        standalone.Render<TimeRecorder>(parameters => parameters.Add(it => it.ViewModel, recorder.Object));

        一覧読み込みの呼び出しを確認する(Times.Once());
    }

    [Fact]
    public void 表示中の記録画面に別のViewModelを渡すと差し替え先の一覧を一度取得します()
    {
        var 差し替え先の記録サービス = new StubTimeRecordService(開始した記録, 終了した記録);

        画面.Render(parameters => parameters.Add(it => it.ViewModel, new TimeRecorderViewModel(差し替え先の記録サービス)));

        差し替え先の記録サービス.ListCount.Should().Be(1);
    }

    [Fact]
    public void 同じViewModelでプロジェクトを切り替えても記録一覧を再取得しません()
    {
        画面.Render(parameters => parameters.Add(it => it.SelectedProject, new Project("プロジェクトA")));

        一覧読み込みの呼び出しを確認する(Times.Once());
    }

    [Fact]
    public void 初期表示では作業記録一覧が空です()
    {
        画面.Find("ul[aria-label='作業記録一覧']").Children.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false, "記録を開始")]
    [InlineData(true, "記録を終了")]
    public void 開始または終了ボタンを押すと記録操作を一度要求します(bool 記録中, string 操作名)
    {
        記録中の状態を用意する(記録中);

        画面.FindAll(".recorder-action button").Single(it => it.TextContent == 操作名).Click();

        記録操作の呼び出しを確認する(Times.Once());
        再取得の呼び出しを確認する(Times.Never());
    }

    [Fact]
    public async Task 記録の開始ボタンを押して開始に失敗すると画面に失敗を表示します()
    {
        var tested = 開始に失敗する記録画面を表示する();

        await tested.Find("button").ClickAsync(new());

        tested.Find("[role='alert']").TextContent.Should().Contain("失敗");
    }

    [Fact]
    public void 開始後の操作欄に開始時刻を表示します()
    {
        開始後の画面.FindAll(".recorder-action time[aria-label='開始時刻']").Should().ContainSingle();
    }

    [Fact]
    public void 記録を開始すると操作欄には終了ボタンだけを表示します()
    {
        開始後の画面.FindAll(".recorder-action button").Should().ContainSingle().Which.TextContent.Should().Be("記録を終了");
    }

    [Fact]
    public void 開始した作業記録を記録一覧に表示します()
    {
        開始後の画面.FindAll("ul[aria-label='作業記録一覧'] li").Should().ContainSingle();
    }

    [Fact]
    public void 終了後は操作欄と作業記録一覧の両方に作業時間を表示します()
    {
        終了後の画面.FindAll(".recorder-action .time-record-duration-value").Should().ContainSingle();
        終了後の画面.FindAll("ul[aria-label='作業記録一覧'] li .time-record-duration-value").Should().ContainSingle();
    }

    [Fact]
    public void 記録を終了すると操作欄には開始ボタンだけを表示します()
    {
        終了後の画面.FindAll(".recorder-action button").Should().ContainSingle().Which.TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 終了後に新しく開始すると操作欄の開始時刻が変わり終了時刻が消えます()
    {
        var tested = 終了後の画面;
        tested.Find(".recorder-action time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 18:00");
        tested.FindAll(".recorder-action time[aria-label='終了時刻']").Should().ContainSingle();

        var 新しく開始した記録 = new TimeRecord(別の記録ID, 終了時刻, null);
        記録一覧を用意する([終了した記録, 新しく開始した記録]);
        操作欄の記録を用意する(新しく開始した記録);
        記録中の状態を用意する(true);
        tested.Render();

        tested.Find(".recorder-action time[aria-label='開始時刻']").TextContent.Should().Be("2026/08/01 19:30");
        tested.FindAll(".recorder-action time[aria-label='終了時刻']").Should().BeEmpty();
    }

    [Fact]
    public void 記録を操作できない間は開始ボタンが無効になり操作できるようになると有効に戻ります()
    {
        記録操作の可否を用意する(false);
        画面.Find("button").HasAttribute("disabled").Should().BeTrue();
        記録操作の可否を用意する(true);
        画面.Render();
        画面.Find("button").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void 記録の最新状態を確認できないときはその旨の警告を表示します()
    {
        最新状態を未確認にする(記録中: false);

        画面.Find("[role='alert']").TextContent.Should().Contain("最新状態を確認できません");
    }

    [Theory]
    [InlineData(false, "記録を開始")]
    [InlineData(true, "記録を終了")]
    public void 記録の最新状態を確認できないときは記録欄の開始または終了ボタンが無効になります(bool 記録中, string 操作名)
    {
        最新状態を未確認にする(記録中);

        画面.FindAll("button").Single(it => it.TextContent == 操作名).HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void 記録の最新状態を確認できないときは一覧の再取得ボタンを押せます()
    {
        最新状態を未確認にする(記録中: false);

        画面.FindAll("button").Single(it => it.TextContent == "再取得").HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public async Task 再取得ボタンを押すと一覧の手動再取得を一度要求します()
    {
        再取得の案内を用意する();

        await 画面.FindAll("button").Single(it => it.TextContent == "再取得").ClickAsync(new());

        一覧読み込みの呼び出しを確認する(Times.Once());
        再取得の呼び出しを確認する(Times.Once());
        記録操作の呼び出しを確認する(Times.Never());
    }

    [Fact]
    public void 一覧を再取得できない間は再取得ボタンを無効にします()
    {
        再取得の案内を用意する();
        再取得の可否を用意する(false);

        画面.FindAll("button").Single(it => it.TextContent == "再取得").HasAttribute("disabled").Should().BeTrue();
    }

    IRenderedComponent<TimeRecorder> 操作結果のある画面を表示する(TimeRecord 記録)
    {
        記録中の状態を用意する(記録.EndedAt is null);
        操作欄の記録を用意する(記録);
        記録一覧を用意する([記録]);
        return 記録画面を表示する(recorder.Object);
    }

    void 最新状態を未確認にする(bool 記録中)
    {
        記録中の状態を用意する(記録中);
        再取得の案内を用意する();
        記録操作の可否を用意する(false);
    }

    IRenderedComponent<TimeRecorder> 開始に失敗する記録画面を表示する() =>
        記録画面を表示する(
            new TimeRecorderViewModel(
                new StubTimeRecordService(開始した記録, 終了した記録)
                {
                    StartResponse = Task.FromException<TimeRecord>(new InvalidOperationException())
                }));

    IRenderedComponent<TimeRecorder> 記録画面を表示する(ITimeRecorderViewModel viewModel) =>
        context.Render<TimeRecorder>(parameters => parameters.Add(it => it.ViewModel, viewModel));

    void 再取得の案内を用意する() => recorder.SetupGet(it => it.Notice).Returns(TimeRecorderNotice.StateUnconfirmed);
    void 一覧読み込みの呼び出しを確認する(Times times) => recorder.Verify(it => it.LoadAsync(), times);
    void 記録操作の可否を用意する(bool canOperate) => recorder.SetupGet(it => it.CanOperate).Returns(canOperate);
    void 記録一覧を用意する(IReadOnlyList<TimeRecord> records) => recorder.SetupGet(it => it.Records).Returns(records);
    void 記録中の状態を用意する(bool isRecording) => recorder.SetupGet(it => it.IsRecording).Returns(isRecording);
    void 操作欄の記録を用意する(TimeRecord record) => recorder.SetupGet(it => it.DisplayedRecord).Returns(record);
    void 再取得の可否を用意する(bool canReload) => recorder.SetupGet(it => it.CanReload).Returns(canReload);
    void 記録操作の呼び出しを確認する(Times times) => recorder.Verify(it => it.ToggleRecordingAsync(), times);
    void 再取得の呼び出しを確認する(Times times) => recorder.Verify(it => it.ReloadRecordsAsync(), times);

    public void Dispose() => context.Dispose();
}
