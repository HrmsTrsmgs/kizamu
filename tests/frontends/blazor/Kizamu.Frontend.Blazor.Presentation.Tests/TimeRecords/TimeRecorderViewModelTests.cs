using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.TimeRecords;

public sealed class TimeRecorderViewModelTests
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly Guid 別の記録ID = Guid.Parse("019cdeac-b600-7000-8000-000000000002");
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    static readonly DateTimeOffset 終了時刻 = DateTimeOffset.Parse("2026-08-01T10:30:00Z");
    readonly StubTimeRecordService service = new(new(記録ID, 開始時刻, null), new(記録ID, 開始時刻, 終了時刻));
    readonly TimeRecorderViewModel tested;

    public TimeRecorderViewModelTests()
    {
        tested = new(service);
    }

    [Fact(Skip = "プロジェクト別の記録操作を実装するとき解除します。")]
    public async Task 異なるプロジェクトの記録を同時に開始できる()
    {
        var プロジェクトA = new Project("プロジェクトA");
        var プロジェクトB = new Project("プロジェクトB");
        tested.SelectProject(プロジェクトA);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(プロジェクトB);
        service.StartedRecord = new(別の記録ID, 終了時刻, null);

        await tested.StartTimeRecordAsync();

        tested.Records.Select(it => it.Id).Should().Equal(記録ID, 別の記録ID);
        tested.IsRecording.Should().BeTrue();
        tested.SelectProject(プロジェクトA);
        tested.IsRecording.Should().BeTrue();
        service.EndedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task 対象を切り替えて戻ると記録中の状態が保たれる()
    {
        var プロジェクトA = new Project("プロジェクトA");
        tested.SelectProject(null);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(プロジェクトA);
        tested.IsRecording.Should().BeFalse();
        service.StartedRecord = new(別の記録ID, 終了時刻, null);
        await tested.StartTimeRecordAsync();

        tested.SelectProject(null);
        tested.IsRecording.Should().BeTrue();
        tested.SelectProject(プロジェクトA);
        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 開始の応答前に対象を切り替えても開始元の記録中状態を更新する()
    {
        var プロジェクトA = new Project("プロジェクトA");
        TaskCompletionSource<TimeRecord> 応答 = new();
        service.StartResponse = 応答.Task;
        tested.SelectProject(プロジェクトA);
        var 開始操作 = tested.StartTimeRecordAsync();
        tested.SelectProject(null);

        応答.SetResult(service.StartedRecord);
        await 開始操作;

        tested.Records.Should().ContainSingle().Which.Should().BeSameAs(service.StartedRecord);
        tested.IsRecording.Should().BeFalse();
        tested.SelectProject(プロジェクトA);
        tested.IsRecording.Should().BeTrue();
    }

    [Theory(Skip = "プロジェクト別の記録操作を実装するとき解除します。")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 一方の記録を終了しても他方は記録中のままになる(bool プロジェクトを終了する)
    {
        var プロジェクトA = new Project("プロジェクトA");
        var 終了する記録ID = プロジェクトを終了する ? 別の記録ID : 記録ID;
        var 残る記録ID = プロジェクトを終了する ? 記録ID : 別の記録ID;
        var (tested, service) = await 未指定対象とプロジェクトで記録中の状態を用意する(プロジェクトA, 終了する記録ID);
        tested.SelectProject(プロジェクトを終了する ? プロジェクトA : null);

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(終了する記録ID);
        tested.IsRecording.Should().BeFalse();
        tested.SelectProject(プロジェクトを終了する ? null : プロジェクトA);
        tested.IsRecording.Should().BeTrue();
        tested.Records.Single(it => it.Id == 残る記録ID).EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task 開始の通信中は追加操作を受け付けず成功後に終了できる()
    {
        TaskCompletionSource<TimeRecord> 応答 = new();
        service.StartResponse = 応答.Task;
        var 開始操作 = tested.StartTimeRecordAsync();
        var 追加開始 = tested.StartTimeRecordAsync();
        var 追加終了 = tested.EndTimeRecordAsync();
        service.StartCount.Should().Be(1);
        service.EndedIds.Should().BeEmpty();

        応答.SetResult(service.StartedRecord);
        await Task.WhenAll(開始操作, 追加開始, 追加終了);
        await tested.EndTimeRecordAsync();
        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 開始の通信中は追加操作を受け付けず失敗後に再開始できる()
    {
        TaskCompletionSource<TimeRecord> 応答 = new();
        service.StartResponse = 応答.Task;
        var 開始操作 = tested.StartTimeRecordAsync();
        var 追加開始 = tested.StartTimeRecordAsync();
        var 追加終了 = tested.EndTimeRecordAsync();
        service.StartCount.Should().Be(1);
        service.EndedIds.Should().BeEmpty();

        応答.SetException(new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => 開始操作);
        await Task.WhenAll(追加開始, 追加終了);
        service.StartResponse = null;
        await tested.StartTimeRecordAsync();
        service.StartCount.Should().Be(2);
    }

    [Fact]
    public async Task 終了の通信中は追加操作を受け付けず成功後に開始できる()
    {
        await tested.StartTimeRecordAsync();
        TaskCompletionSource<TimeRecord> 応答 = new();
        service.EndResponse = 応答.Task;
        var 終了操作 = tested.EndTimeRecordAsync();
        var 追加開始 = tested.StartTimeRecordAsync();
        var 追加終了 = tested.EndTimeRecordAsync();
        service.StartCount.Should().Be(1);
        service.EndedIds.Should().Equal(記録ID);

        応答.SetResult(service.EndedRecord);
        await Task.WhenAll(終了操作, 追加開始, 追加終了);
        await tested.StartTimeRecordAsync();
        service.StartCount.Should().Be(2);
    }

    [Fact]
    public async Task 終了の通信中は追加操作を受け付けず失敗後に再終了できる()
    {
        await tested.StartTimeRecordAsync();
        service.Records = [service.StartedRecord];
        TaskCompletionSource<TimeRecord> 応答 = new();
        service.EndResponse = 応答.Task;
        var 終了操作 = tested.EndTimeRecordAsync();
        var 追加開始 = tested.StartTimeRecordAsync();
        var 追加終了 = tested.EndTimeRecordAsync();
        service.StartCount.Should().Be(1);
        service.EndedIds.Should().Equal(記録ID);

        応答.SetException(new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => 終了操作);
        await Task.WhenAll(追加開始, 追加終了);
        service.EndResponse = null;
        await tested.EndTimeRecordAsync();
        service.EndedIds.Should().Equal(記録ID, 記録ID);
    }

    [Fact]
    public async Task 開始の通信に失敗すると一覧を再取得して反映する()
    {
        service.Records = [service.StartedRecord];
        service.StartResponse = Task.FromException<TimeRecord>(new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.StartTimeRecordAsync());

        service.ListCount.Should().Be(1);
        tested.Records.Should().Equal(service.StartedRecord);
        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 終了の通信に失敗すると一覧を再取得して反映する()
    {
        await tested.StartTimeRecordAsync();
        service.Records = [service.EndedRecord];
        service.EndResponse = Task.FromException<TimeRecord>(new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.EndTimeRecordAsync());

        service.ListCount.Should().Be(1);
        tested.Records.Should().Equal(service.EndedRecord);
        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 開始失敗後の一覧再取得にも失敗すると直前の一覧を残して未確認状態になる()
    {
        await 終了済みの履歴を読み込む();
        service.StartResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        一覧取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.StartTimeRecordAsync());

        tested.Records.Should().Equal(service.Records);
        tested.IsStateUnconfirmed.Should().BeTrue();
        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 終了失敗後の一覧再取得にも失敗すると直前の一覧を残して未確認状態になる()
    {
        await tested.StartTimeRecordAsync();
        service.EndResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        一覧取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.EndTimeRecordAsync());

        tested.Records.Should().Equal(service.StartedRecord);
        tested.IsStateUnconfirmed.Should().BeTrue();
        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 開始失敗後の状態を確認できるまでは追加の開始と終了を要求しない()
    {
        await 開始失敗後の再取得にも失敗させる();
        service.StartResponse = null;

        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();

        service.StartCount.Should().Be(1);
        service.EndedIds.Should().BeEmpty();
        service.ListCount.Should().Be(1);
    }

    [Fact]
    public async Task 終了失敗後の状態を確認できるまでは追加の終了と開始を要求しない()
    {
        await 終了失敗後の再取得にも失敗させる();
        service.EndResponse = null;

        await tested.EndTimeRecordAsync();
        await tested.StartTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
        service.StartCount.Should().Be(1);
        service.ListCount.Should().Be(1);
    }

    [Fact]
    public async Task 手動再取得に成功すると取得した記録を終了対象として操作を再開できる()
    {
        await 開始失敗後の再取得にも失敗させる();
        service.DelayListResponse().SetResult([service.StartedRecord]);

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.StartedRecord);
        tested.IsStateUnconfirmed.Should().BeFalse();
        tested.IsRecording.Should().BeTrue();
        await tested.EndTimeRecordAsync();
        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 手動再取得で終了済みの一覧を確認すると開始できる()
    {
        await 終了失敗後の再取得にも失敗させる();
        service.DelayListResponse().SetResult([service.EndedRecord]);

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.EndedRecord);
        tested.IsStateUnconfirmed.Should().BeFalse();
        tested.IsRecording.Should().BeFalse();
        await tested.StartTimeRecordAsync();
        service.StartCount.Should().Be(2);
    }

    [Fact]
    public async Task 手動再取得にも失敗すると直前の一覧と未確認状態を保つ()
    {
        await 終了済みの履歴を読み込む();
        await 開始失敗後の再取得にも失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        tested.Records.Should().Equal(service.Records);
        tested.IsStateUnconfirmed.Should().BeTrue();
        tested.IsBusy.Should().BeFalse();
        service.ListCount.Should().Be(3);
    }

    [Fact]
    public async Task 手動再取得の通信中は再取得と開始と終了の追加要求を送らない()
    {
        await 開始失敗後の再取得にも失敗させる();
        service.StartResponse = null;
        var 応答 = service.DelayListResponse();

        var 再取得 = tested.LoadAsync();
        var 重複再取得 = tested.LoadAsync();
        var 再取得中 = tested.IsBusy;
        var 一覧要求数 = service.ListCount;
        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();
        応答.SetResult([service.EndedRecord]);
        await Task.WhenAll(再取得, 重複再取得);

        再取得中.Should().BeTrue();
        一覧要求数.Should().Be(2);
        service.StartCount.Should().Be(1);
        service.EndedIds.Should().BeEmpty();
    }

    [Fact]
    public void 初期状態は空の一覧で操作結果がなく記録していない()
    {
        tested.Records.Should().BeEmpty();
        tested.DisplayedRecord.Should().BeNull();
        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 読み込んだ記録は順序を保ち記録中なら終了操作の対象になる()
    {
        service.Records = [new(別の記録ID, 開始時刻, 終了時刻), new(記録ID, 開始時刻, null)];

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.Records);
        tested.IsRecording.Should().BeTrue();
        tested.DisplayedRecord.Should().BeNull();
    }

    [Fact]
    public async Task 終了済みの記録だけを読み込むと記録していない状態になる()
    {
        service.Records = [new(記録ID, 開始時刻, 終了時刻)];

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.Records);
        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 開始すると確定した記録が一覧の末尾と操作欄に反映される()
    {
        await 終了済みの履歴を読み込む();

        await tested.StartTimeRecordAsync();

        tested.Records.Should().Equal(service.Records[0], service.StartedRecord);
        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 開始後に古い一覧が届いても過去の記録と開始した記録が残り終了できる()
    {
        var 過去の記録 = new TimeRecord(別の記録ID, 開始時刻, 終了時刻);
        var 初期一覧の応答 = service.DelayListResponse();

        var 初期一覧の読み込み = tested.LoadAsync();
        await tested.StartTimeRecordAsync();
        初期一覧の応答.SetResult([過去の記録]);
        await 初期一覧の読み込み;

        tested.Records.Should().Equal(過去の記録, service.StartedRecord);
        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
        tested.IsRecording.Should().BeTrue();

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
        tested.Records.Should().Equal(過去の記録, service.EndedRecord);
        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 操作後の再読み込みはサーバーの更新と削除を反映する()
    {
        await 履歴がある状態で記録を開始する();
        service.Records = [service.EndedRecord];

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.EndedRecord);
        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 終了すると開始した記録だけが更新され操作結果が残る()
    {
        await 履歴がある状態で記録を開始する();

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
        tested.Records.Should().Equal(service.Records[0], service.EndedRecord);
        tested.DisplayedRecord.Should().BeSameAs(service.EndedRecord);
        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 再び開始すると操作結果を差し替え終了済みの履歴は残る()
    {
        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();
        service.StartedRecord = new(別の記録ID, 終了時刻, null);

        await tested.StartTimeRecordAsync();

        tested.Records.Should().Equal(service.EndedRecord, service.StartedRecord);
        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
        tested.DisplayedRecord.EndedAt.Should().BeNull();
        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 読み込んだ記録を終了しても操作欄に直近の結果は表示しない()
    {
        service.Records = [new(別の記録ID, 開始時刻, 終了時刻), new(記録ID, 開始時刻, null)];
        await tested.LoadAsync();

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
        tested.Records.Should().Equal(service.Records[0], service.EndedRecord);
        tested.DisplayedRecord.Should().BeNull();
        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 終了対象がなければサービスへ終了を要求しない()
    {
        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().BeEmpty();
        tested.Records.Should().BeEmpty();
        tested.DisplayedRecord.Should().BeNull();
        tested.IsRecording.Should().BeFalse();
    }

    void 一覧取得を失敗させる() => service.DelayListResponse().SetException(new HttpRequestException());

    async Task 開始失敗後の再取得にも失敗させる()
    {
        service.StartResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        一覧取得を失敗させる();
        await Assert.ThrowsAsync<HttpRequestException>(() => tested.StartTimeRecordAsync());
    }

    async Task 終了失敗後の再取得にも失敗させる()
    {
        await tested.StartTimeRecordAsync();
        service.EndResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        一覧取得を失敗させる();
        await Assert.ThrowsAsync<HttpRequestException>(() => tested.EndTimeRecordAsync());
    }

    async Task 終了済みの履歴を読み込む()
    {
        service.Records = [new(別の記録ID, 開始時刻, 終了時刻)];
        await tested.LoadAsync();
    }

    async Task 履歴がある状態で記録を開始する()
    {
        await 終了済みの履歴を読み込む();
        await tested.StartTimeRecordAsync();
    }

    static async Task<(TimeRecorderViewModel tested, StubTimeRecordService service)> 未指定対象とプロジェクトで記録中の状態を用意する(
        Project プロジェクト, Guid 終了する記録ID)
    {
        var service = new StubTimeRecordService(
            new(記録ID, 開始時刻, null), new(終了する記録ID, 開始時刻, 終了時刻));
        var tested = new TimeRecorderViewModel(service);
        tested.SelectProject(null);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(プロジェクト);
        service.StartedRecord = new(別の記録ID, 開始時刻, null);
        await tested.StartTimeRecordAsync();
        return (tested, service);
    }
}
