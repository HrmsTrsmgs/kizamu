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
    readonly Project プロジェクトA = new("プロジェクトA");
    readonly TimeRecord 過去の記録 = new(別の記録ID, 開始時刻, 終了時刻);
    readonly StubTimeRecordService service = new(new(記録ID, 開始時刻, null), new(記録ID, 開始時刻, 終了時刻));
    readonly TimeRecorderViewModel tested;

    public TimeRecorderViewModelTests()
    {
        tested = new(service);
    }

    [Fact]
    public async Task 開始中は利用者の操作を止めて失敗後に再開する()
    {
        var 応答 = service.DelayStartResponse();

        var 開始 = tested.RunUserOperationAsync(tested.StartTimeRecordAsync);
        tested.CanOperate.Should().BeFalse();
        応答.SetException(new InvalidOperationException());
        await 開始;

        tested.CanOperate.Should().BeTrue();
    }

    [Fact]
    public async Task 利用者の操作失敗は応答が失敗してから表示する()
    {
        var 応答 = service.DelayStartResponse();

        var 開始 = tested.RunUserOperationAsync(tested.StartTimeRecordAsync);
        tested.HasOperationFailed.Should().BeFalse();
        応答.SetException(new InvalidOperationException());
        await 開始;

        tested.HasOperationFailed.Should().BeTrue();
    }

    [Fact]
    public async Task 利用者の操作が次に成功しても前の失敗表示を保つ()
    {
        service.StartResponse = Task.FromException<TimeRecord>(new InvalidOperationException());
        await tested.RunUserOperationAsync(tested.StartTimeRecordAsync);
        tested.HasOperationFailed.Should().BeTrue();
        service.StartResponse = null;

        await tested.RunUserOperationAsync(tested.StartTimeRecordAsync);

        tested.IsRecording.Should().BeTrue();
        tested.HasOperationFailed.Should().BeTrue();
    }

    [Fact]
    public async Task 最新状態が未確認の間は再取得を案内し回復後は以前の操作失敗を知らせる()
    {
        service.StartResponse = Task.FromException<TimeRecord>(new InvalidOperationException());
        一覧取得を失敗させる(new InvalidOperationException());
        tested.Notice.Should().Be(TimeRecorderNotice.None);

        await tested.RunUserOperationAsync(tested.StartTimeRecordAsync);

        tested.Notice.Should().Be(TimeRecorderNotice.StateUnconfirmed);
        service.DelayListResponse().SetResult([]);
        await tested.RunUserOperationAsync(tested.LoadAsync);
        tested.Notice.Should().Be(TimeRecorderNotice.OperationFailed);
    }

    [Fact]
    public async Task 利用者による手動再取得の失敗を表示する()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.HasOperationFailed.Should().BeTrue();
    }

    [Fact]
    public async Task 利用者による手動再取得に失敗しても未確認状態を保つ()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task 利用者による手動再取得に失敗した後も記録操作を止める()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.CanOperate.Should().BeFalse();
    }

    [Fact]
    public async Task 利用者による手動再取得が失敗すると通信中の状態を解除する()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 初回の一覧取得に失敗すると元の例外を返す()
    {
        var 取得失敗 = new InvalidOperationException();
        一覧取得を失敗させる(取得失敗);

        (await Assert.ThrowsAsync<InvalidOperationException>(tested.LoadAsync)).Should().BeSameAs(取得失敗);
    }

    [Fact]
    public async Task 初回の一覧取得に失敗しても操作失敗を表示しない()
    {
        一覧取得を失敗させる(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(tested.LoadAsync);

        tested.HasOperationFailed.Should().BeFalse();
    }

    [Fact]
    public async Task 異なるプロジェクトで開始した記録を両方とも一覧に残す()
    {
        await プロジェクトAとBで記録を開始する();

        tested.Records.Select(it => it.Id).Should().Equal(記録ID, 別の記録ID);
    }

    [Fact]
    public async Task 異なるプロジェクトで記録を開始すると選択中のプロジェクトが記録中になる()
    {
        await プロジェクトAとBで記録を開始する();

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 別のプロジェクトで記録を開始して戻っても元の記録を終了しない()
    {
        await プロジェクトAとBで記録を開始する();

        tested.SelectProject(プロジェクトA);

        service.EndedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task プロジェクトを切り替えて戻ると元の記録中状態を保持する()
    {
        await プロジェクトAとBで記録を開始する();

        tested.SelectProject(プロジェクトA);

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 未指定からプロジェクトへ切り替えて戻ると元の記録中状態を保持する()
    {
        tested.SelectProject(null);
        await tested.StartTimeRecordAsync();

        tested.SelectProject(プロジェクトA);
        tested.IsRecording.Should().BeFalse();
        service.StartedRecord = new(別の記録ID, 終了時刻, null);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(null);

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task プロジェクトから未指定へ切り替えて戻ると元の記録中状態を保持する()
    {
        await 未指定とプロジェクトの記録を開始する(プロジェクトA);
        tested.SelectProject(null);

        tested.SelectProject(プロジェクトA);

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 開始の応答前に対象を切り替えても確定した記録を一覧に加える()
    {
        var (応答, 開始操作) = プロジェクトAの開始応答を待つ間に未指定へ切り替える();

        応答.SetResult(service.StartedRecord);
        await 開始操作;

        tested.Records.Should().ContainSingle().Which.Should().BeSameAs(service.StartedRecord);
    }

    [Fact]
    public async Task 開始の応答前に対象を切り替えても開始元だけを記録中にする()
    {
        var (応答, 開始操作) = プロジェクトAの開始応答を待つ間に未指定へ切り替える();

        応答.SetResult(service.StartedRecord);
        await 開始操作;

        tested.IsRecording.Should().BeFalse();
        tested.SelectProject(プロジェクトA);
        tested.IsRecording.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 選択した対象の記録IDで終了を依頼する(bool プロジェクトを終了する)
    {
        var (終了する記録ID, _, _) = await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(終了する記録ID);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 選択した対象の記録を終了すると記録中ではなくなる(bool プロジェクトを終了する)
    {
        await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 一方の記録を終了しても他方へ切り替えると記録中のままである(bool プロジェクトを終了する)
    {
        var (_, 残るプロジェクト, _) = await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();
        tested.SelectProject(残るプロジェクト);

        tested.IsRecording.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 一方の記録を終了しても一覧の他方の記録に終了時刻を付けない(bool プロジェクトを終了する)
    {
        var (_, _, 残る記録ID) = await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();

        tested.Records.Single(it => it.Id == 残る記録ID).EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task 開始の通信中は追加の開始と終了を要求しない()
    {
        var 応答 = service.DelayStartResponse();
        var 開始操作 = tested.StartTimeRecordAsync();

        var 追加操作 = 開始と終了を続けて要求する();

        service.StartCount.Should().Be(1);
        service.EndedIds.Should().BeEmpty();

        応答.SetResult(service.StartedRecord);
        await Task.WhenAll(開始操作, 追加操作);
    }

    [Fact]
    public async Task 開始の通信に成功した後は記録を終了できる()
    {
        var 応答 = service.DelayStartResponse();
        var 開始操作 = tested.StartTimeRecordAsync();
        var 追加操作 = 開始と終了を続けて要求する();

        応答.SetResult(service.StartedRecord);
        await Task.WhenAll(開始操作, 追加操作);

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 開始の通信に失敗した後は記録を再開できる()
    {
        var 応答 = service.DelayStartResponse();
        var 開始操作 = tested.StartTimeRecordAsync();
        var 追加操作 = 開始と終了を続けて要求する();

        応答.SetException(new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => 開始操作);
        await 追加操作;
        service.StartResponse = null;

        await tested.StartTimeRecordAsync();

        service.StartCount.Should().Be(2);
    }

    [Fact]
    public async Task 終了の通信中は追加の開始と終了を要求しない()
    {
        await tested.StartTimeRecordAsync();
        var 応答 = service.DelayEndResponse();
        var 終了操作 = tested.EndTimeRecordAsync();

        var 追加操作 = 開始と終了を続けて要求する();

        service.StartCount.Should().Be(1);
        service.EndedIds.Should().Equal(記録ID);

        応答.SetResult(service.EndedRecord);
        await Task.WhenAll(終了操作, 追加操作);
    }

    [Fact]
    public async Task 終了の通信に成功した後は新しい記録を開始できる()
    {
        await tested.StartTimeRecordAsync();
        var 応答 = service.DelayEndResponse();
        var 終了操作 = tested.EndTimeRecordAsync();
        var 追加操作 = 開始と終了を続けて要求する();

        応答.SetResult(service.EndedRecord);
        await Task.WhenAll(終了操作, 追加操作);

        await tested.StartTimeRecordAsync();

        service.StartCount.Should().Be(2);
    }

    [Fact]
    public async Task 終了の通信に失敗した後は終了を再び要求できる()
    {
        await tested.StartTimeRecordAsync();
        service.Records = [service.StartedRecord];
        var 応答 = service.DelayEndResponse();
        var 終了操作 = tested.EndTimeRecordAsync();
        var 追加操作 = 開始と終了を続けて要求する();

        応答.SetException(new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => 終了操作);
        await 追加操作;
        service.EndResponse = null;

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID, 記録ID);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作の通信に失敗すると再取得の成功後に元の例外を返す(bool 終了操作)
    {
        var 通信失敗 = new HttpRequestException();
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, 通信失敗);

        (await Assert.ThrowsAsync<HttpRequestException>(操作)).Should().BeSameAs(通信失敗);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作の通信に失敗すると一覧を一度再取得する(bool 終了操作)
    {
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        service.ListCount.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作の通信に失敗すると再取得した記録を一覧に反映する(bool 終了操作)
    {
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.Records.Should().Equal(service.Records);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作の通信に失敗すると再取得した記録の状態を操作対象に反映する(bool 終了操作)
    {
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.IsRecording.Should().Be(!終了操作);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作とその一覧再取得が失敗すると再取得の例外を返す(bool 終了操作)
    {
        var 再取得失敗 = new HttpRequestException();
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, 再取得失敗);

        (await Assert.ThrowsAsync<HttpRequestException>(操作)).Should().BeSameAs(再取得失敗);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作とその一覧再取得が失敗すると直前の一覧を残す(bool 終了操作)
    {
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, new HttpRequestException());
        IEnumerable<TimeRecord> 直前の記録 = 終了操作 ? [service.StartedRecord] : service.Records;

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.Records.Should().Equal(直前の記録);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作とその一覧再取得が失敗すると最新状態は未確認になる(bool 終了操作)
    {
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録操作とその一覧再取得が失敗すると通信中の状態を解除する(bool 終了操作)
    {
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 開始失敗後に古い初回一覧が届いても未確認状態を保つ()
    {
        await 開始失敗後に古い初回一覧を受け取る();

        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task 開始失敗後に古い初回一覧が届いても記録操作を抑止する()
    {
        await 開始失敗後に古い初回一覧を受け取る();

        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();

        service.StartCount.Should().Be(1);
        service.EndedIds.Should().BeEmpty();
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 手動再取得が成功すると取得した記録を一覧に反映する(bool 記録中)
    {
        await 手動再取得で返す記録を用意する(記録中);

        await tested.LoadAsync();

        tested.Records.Should().Equal(記録中 ? service.StartedRecord : service.EndedRecord);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 手動再取得が成功すると未確認状態を解除する(bool 記録中)
    {
        await 手動再取得で返す記録を用意する(記録中);

        await tested.LoadAsync();

        tested.IsStateUnconfirmed.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 手動再取得で確認した記録を操作対象の状態に反映する(bool 記録中)
    {
        await 手動再取得で返す記録を用意する(記録中);

        await tested.LoadAsync();

        tested.IsRecording.Should().Be(記録中);
    }

    [Fact]
    public async Task 手動再取得で確認した未終了の記録を終了できる()
    {
        await 手動再取得で返す記録を用意する(記録中: true);
        await tested.LoadAsync();

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 手動再取得で終了済みの記録を確認すると開始できる()
    {
        await 手動再取得で返す記録を用意する(記録中: false);
        await tested.LoadAsync();

        await tested.StartTimeRecordAsync();

        service.StartCount.Should().Be(2);
    }

    [Fact]
    public async Task 手動再取得にも失敗すると直前の一覧を残す()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        tested.Records.Should().Equal(service.Records);
    }

    [Fact]
    public async Task 手動再取得にも失敗すると未確認状態を保つ()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task 手動再取得にも失敗すると通信中の状態を解除する()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 開始後の自動再取得に失敗しても手動再取得で一覧を再要求する()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        service.ListCount.Should().Be(3);
    }

    [Fact]
    public async Task 手動再取得の応答待ちは通信中として扱う()
    {
        await 開始失敗後の再取得にも失敗させる();
        service.StartResponse = null;
        var 応答 = service.DelayListResponse();

        var 再取得 = tested.LoadAsync();

        tested.IsBusy.Should().BeTrue();

        応答.SetResult([service.EndedRecord]);
        await 再取得;
    }

    [Fact]
    public async Task 手動再取得の応答待ちに重ねて再取得しても追加の一覧要求を送らない()
    {
        await 開始失敗後の再取得にも失敗させる();
        service.StartResponse = null;
        var 応答 = service.DelayListResponse();

        var 再取得 = tested.LoadAsync();
        var 重複再取得 = tested.LoadAsync();

        service.ListCount.Should().Be(2);

        応答.SetResult([service.EndedRecord]);
        await Task.WhenAll(再取得, 重複再取得);
    }

    [Fact]
    public async Task 手動再取得の通信中は開始と終了を要求しない()
    {
        await 開始失敗後の再取得にも失敗させる();
        service.StartResponse = null;
        var 応答 = service.DelayListResponse();

        var 再取得 = tested.LoadAsync();
        var 重複再取得 = tested.LoadAsync();

        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();
        応答.SetResult([service.EndedRecord]);
        await Task.WhenAll(再取得, 重複再取得);

        service.StartCount.Should().Be(1);
        service.EndedIds.Should().BeEmpty();
    }

    [Fact]
    public void 初期状態の記録一覧は空である()
    {
        tested.Records.Should().BeEmpty();
    }

    [Fact]
    public void 初期状態の操作欄に記録はない()
    {
        tested.DisplayedRecord.Should().BeNull();
    }

    [Fact]
    public void 初期状態は記録中ではない()
    {
        tested.IsRecording.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 読み込んだ順序のまま記録一覧に反映する(bool 記録中の項目を含む)
    {
        読み込む記録一覧を用意する(記録中の項目を含む);

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.Records);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 読み込んだ一覧に未終了の記録があれば記録中になる(bool 記録中の項目を含む)
    {
        読み込む記録一覧を用意する(記録中の項目を含む);

        await tested.LoadAsync();

        tested.IsRecording.Should().Be(記録中の項目を含む);
    }

    [Fact]
    public async Task 記録中の項目を読み込んでも操作欄には表示しない()
    {
        読み込む記録一覧を用意する(記録中の項目を含む: true);

        await tested.LoadAsync();

        tested.DisplayedRecord.Should().BeNull();
    }

    [Fact]
    public async Task 開始すると確定した記録を一覧の末尾に加える()
    {
        await 終了済みの履歴を読み込む();

        await tested.StartTimeRecordAsync();

        tested.Records.Should().Equal(service.Records[0], service.StartedRecord);
    }

    [Fact]
    public async Task 開始すると確定した記録を操作欄に表示する()
    {
        await 終了済みの履歴を読み込む();

        await tested.StartTimeRecordAsync();

        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
    }

    [Fact]
    public async Task 開始すると操作対象が記録中になる()
    {
        await 終了済みの履歴を読み込む();

        await tested.StartTimeRecordAsync();

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 開始後に古い初回一覧が届くと過去の記録の後に開始した記録が残る()
    {
        await 記録開始後に古い初回一覧を受け取る();

        tested.Records.Should().Equal(過去の記録, service.StartedRecord);
    }

    [Fact]
    public async Task 開始後に古い初回一覧が届いても操作欄には開始した記録が残る()
    {
        await 記録開始後に古い初回一覧を受け取る();

        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
    }

    [Fact]
    public async Task 開始後に古い初回一覧が届いても終了するまで記録中のままである()
    {
        await 記録開始後に古い初回一覧を受け取る();

        tested.IsRecording.Should().BeTrue();

        await tested.EndTimeRecordAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 開始後に古い初回一覧が届いても開始した記録のIDで終了を依頼する()
    {
        await 記録開始後に古い初回一覧を受け取る();

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 開始後に古い初回一覧が届いても終了時に過去の記録を残す()
    {
        await 記録開始後に古い初回一覧を受け取る();

        await tested.EndTimeRecordAsync();

        tested.Records.Should().Equal(過去の記録, service.EndedRecord);
    }

    [Fact]
    public async Task 古い一覧と同じIDの記録は最新の操作結果で置き換える()
    {
        await 記録の更新と別の記録の開始後に古い一覧を受け取る();

        tested.Records.Where(it => it.Id == 記録ID).Should().Equal(service.EndedRecord);
    }

    [Fact]
    public async Task 古い一覧にない記録を応答内の記録の後に加える()
    {
        await 記録の更新と別の記録の開始後に古い一覧を受け取る();

        tested.Records.Select(it => it.Id).Should().Equal(記録ID, 別の記録ID);
        tested.Records[^1].Should().Be(service.StartedRecord);
    }

    [Fact]
    public async Task 古い一覧にない記録は最新の更新を最初の操作順で残す()
    {
        var 応答 = service.DelayListResponse();
        var 読み込み = tested.LoadAsync();
        await tested.StartTimeRecordAsync();
        tested.SelectProject(new("別のプロジェクト"));
        service.StartedRecord = new(別の記録ID, 開始時刻, null);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(null);
        await tested.EndTimeRecordAsync();

        応答.SetResult([]);
        await 読み込み;

        tested.Records.Should().Equal(service.EndedRecord, service.StartedRecord);
    }

    [Fact]
    public async Task 古い一覧の先頭にも記録中の項目があれば取得中に開始した記録を先に終了する()
    {
        var 応答 = service.DelayListResponse();
        var 読み込み = tested.LoadAsync();
        await tested.StartTimeRecordAsync();
        応答.SetResult([new(別の記録ID, 開始時刻, null)]);
        await 読み込み;

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 操作後に一覧を再読み込みするとサーバーの最新の一覧で置き換える()
    {
        await 履歴がある状態で記録を開始する();
        service.Records = [service.EndedRecord];

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.EndedRecord);
    }

    [Fact]
    public async Task 操作後に再読み込みした記録が終了済みなら操作対象は記録中ではなくなる()
    {
        await 履歴がある状態で記録を開始する();
        service.Records = [service.EndedRecord];

        await tested.LoadAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 終了すると対象の記録IDをサービスに渡す(bool 一覧から読み込む)
    {
        await 終了対象の記録を用意する(一覧から読み込む);

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 終了すると履歴を保って対象の記録だけを更新する(bool 一覧から読み込む)
    {
        await 終了対象の記録を用意する(一覧から読み込む);

        await tested.EndTimeRecordAsync();

        tested.Records.Should().Equal(service.Records[0], service.EndedRecord);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 終了すると操作対象は記録中ではなくなる(bool 一覧から読み込む)
    {
        await 終了対象の記録を用意する(一覧から読み込む);

        await tested.EndTimeRecordAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 自分で開始した記録を終了すると操作欄に終了結果が残る()
    {
        await 履歴がある状態で記録を開始する();

        await tested.EndTimeRecordAsync();

        tested.DisplayedRecord.Should().BeSameAs(service.EndedRecord);
    }

    [Fact]
    public async Task 再び開始すると終了済みの履歴の後に新しい記録を加える()
    {
        await 記録を終了して次に別の記録を開始できるようにする();

        await tested.StartTimeRecordAsync();

        tested.Records.Should().Equal(service.EndedRecord, service.StartedRecord);
    }

    [Fact]
    public async Task 再び開始すると操作欄を新しい未終了の記録に差し替える()
    {
        await 記録を終了して次に別の記録を開始できるようにする();

        await tested.StartTimeRecordAsync();

        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
        tested.DisplayedRecord.EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task 再び開始すると操作対象は記録中に戻る()
    {
        await 記録を終了して次に別の記録を開始できるようにする();

        await tested.StartTimeRecordAsync();

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 読み込んだ記録を終了しても操作欄に直近の結果は表示しない()
    {
        await 終了対象の記録を用意する(一覧から読み込む: true);

        await tested.EndTimeRecordAsync();

        tested.DisplayedRecord.Should().BeNull();
    }

    [Fact]
    public async Task 終了対象がなければサービスへ終了を要求しない()
    {
        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task 終了対象がないときの終了操作では画面状態を変えない()
    {
        await tested.EndTimeRecordAsync();

        tested.Records.Should().BeEmpty();
        tested.DisplayedRecord.Should().BeNull();
        tested.IsRecording.Should().BeFalse();
    }

    (TaskCompletionSource<TimeRecord> 応答, Task 開始操作) プロジェクトAの開始応答を待つ間に未指定へ切り替える()
    {
        var 応答 = service.DelayStartResponse();
        tested.SelectProject(プロジェクトA);
        var 開始操作 = tested.StartTimeRecordAsync();
        tested.SelectProject(null);
        return (応答, 開始操作);
    }

    async Task<(Guid 終了する記録ID, Project? 残るプロジェクト, Guid 残る記録ID)> 未指定とプロジェクトの記録から終了対象を選ぶ(bool プロジェクトを終了する)
    {
        var (終了するプロジェクト, 終了する記録ID, 残るプロジェクト, 残る記録ID) =
            プロジェクトを終了する
                ? (プロジェクトA, 別の記録ID, (Project?)null, 記録ID)
                : ((Project?)null, 記録ID, プロジェクトA, 別の記録ID);
        await 未指定とプロジェクトの記録を開始する(プロジェクトA);
        終了時の応答を用意する(終了する記録ID);
        tested.SelectProject(終了するプロジェクト);
        return (終了する記録ID, 残るプロジェクト, 残る記録ID);
    }

    Task 開始と終了を続けて要求する() =>
        Task.WhenAll(tested.StartTimeRecordAsync(), tested.EndTimeRecordAsync());

    async Task<Func<Task>> 再取得で結果を確認できる通信失敗を用意する(bool 終了操作, Exception 通信失敗)
    {
        if (終了操作)
        {
            await tested.StartTimeRecordAsync();
            service.Records = [service.EndedRecord];
            service.EndResponse = Task.FromException<TimeRecord>(通信失敗);
            return tested.EndTimeRecordAsync;
        }

        service.Records = [service.StartedRecord];
        service.StartResponse = Task.FromException<TimeRecord>(通信失敗);
        return tested.StartTimeRecordAsync;
    }

    async Task<Func<Task>> 再取得にも失敗する記録操作を用意する(bool 終了操作, Exception 再取得失敗)
    {
        if (終了操作)
        {
            await tested.StartTimeRecordAsync();
            service.EndResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        }
        else
        {
            await 終了済みの履歴を読み込む();
            service.StartResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        }

        一覧取得を失敗させる(再取得失敗);
        return 終了操作 ? tested.EndTimeRecordAsync : tested.StartTimeRecordAsync;
    }

    async Task 開始失敗後に古い初回一覧を受け取る()
    {
        var 初期一覧の応答 = service.DelayListResponse();
        var 初期一覧の読み込み = tested.LoadAsync();
        service.StartResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        一覧取得を失敗させる(new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.StartTimeRecordAsync());
        初期一覧の応答.SetResult([]);
        await 初期一覧の読み込み;
        service.StartResponse = null;
    }

    async Task 手動再取得で返す記録を用意する(bool 記録中)
    {
        if (記録中)
        {
            await 開始失敗後の再取得にも失敗させる();
            service.DelayListResponse().SetResult([service.StartedRecord]);
        }
        else
        {
            await 終了失敗後の再取得にも失敗させる();
            service.DelayListResponse().SetResult([service.EndedRecord]);
        }
    }

    void 読み込む記録一覧を用意する(bool 記録中の項目を含む) =>
        service.Records = 記録中の項目を含む
            ? [過去の記録, new(記録ID, 開始時刻, null)]
            : [new(記録ID, 開始時刻, 終了時刻)];

    async Task 記録開始後に古い初回一覧を受け取る()
    {
        var 初期一覧の応答 = service.DelayListResponse();

        var 初期一覧の読み込み = tested.LoadAsync();
        await tested.StartTimeRecordAsync();
        初期一覧の応答.SetResult([過去の記録]);
        await 初期一覧の読み込み;
    }

    async Task 記録の更新と別の記録の開始後に古い一覧を受け取る()
    {
        var 先に開始した記録 = service.StartedRecord;
        var 応答 = service.DelayListResponse();
        var 読み込み = tested.LoadAsync();
        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();
        service.StartedRecord = new(別の記録ID, 終了時刻, null);
        await tested.StartTimeRecordAsync();

        応答.SetResult([先に開始した記録]);
        await 読み込み;
    }

    async Task 記録を終了して次に別の記録を開始できるようにする()
    {
        await tested.StartTimeRecordAsync();
        await tested.EndTimeRecordAsync();
        service.StartedRecord = new(別の記録ID, 終了時刻, null);
    }

    async Task 終了対象の記録を用意する(bool 一覧から読み込む)
    {
        if (一覧から読み込む)
        {
            読み込む記録一覧を用意する(記録中の項目を含む: true);
            await tested.LoadAsync();
        }
        else
        {
            await 履歴がある状態で記録を開始する();
        }
    }

    async Task プロジェクトAとBで記録を開始する()
    {
        tested.SelectProject(プロジェクトA);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(new("プロジェクトB"));
        service.StartedRecord = new(別の記録ID, 終了時刻, null);
        await tested.StartTimeRecordAsync();
    }

    async Task 未指定とプロジェクトの記録を開始する(Project project)
    {
        tested.SelectProject(null);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(project);
        service.StartedRecord = new(別の記録ID, 開始時刻, null);
        await tested.StartTimeRecordAsync();
    }

    void 一覧取得を失敗させる(Exception error) => service.DelayListResponse().SetException(error);

    async Task 開始失敗後の再取得にも失敗させる()
    {
        service.StartResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        一覧取得を失敗させる(new HttpRequestException());
        await Assert.ThrowsAsync<HttpRequestException>(() => tested.StartTimeRecordAsync());
    }

    async Task 終了失敗後の再取得にも失敗させる()
    {
        await tested.StartTimeRecordAsync();
        service.EndResponse = Task.FromException<TimeRecord>(new HttpRequestException());
        一覧取得を失敗させる(new HttpRequestException());
        await Assert.ThrowsAsync<HttpRequestException>(() => tested.EndTimeRecordAsync());
    }

    async Task 終了済みの履歴がある状態で開始と再取得を失敗させる()
    {
        await 終了済みの履歴を読み込む();
        await 開始失敗後の再取得にも失敗させる();
    }

    async Task 終了済みの履歴を読み込む()
    {
        service.Records = [過去の記録];
        await tested.LoadAsync();
    }

    async Task 履歴がある状態で記録を開始する()
    {
        await 終了済みの履歴を読み込む();
        await tested.StartTimeRecordAsync();
    }

    void 終了時の応答を用意する(Guid id) =>
        service.EndResponse = Task.FromResult<TimeRecord>(new(id, 開始時刻, 終了時刻));
}
