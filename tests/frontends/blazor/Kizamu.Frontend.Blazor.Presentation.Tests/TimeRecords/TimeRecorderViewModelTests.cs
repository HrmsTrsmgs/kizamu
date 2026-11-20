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
    readonly Project プロジェクトB = new("プロジェクトB");
    readonly TimeRecord 過去の記録 = new(別の記録ID, 開始時刻, 終了時刻);
    readonly StubTimeRecordService service = new(new(記録ID, 開始時刻, null), new(記録ID, 開始時刻, 終了時刻));
    readonly TimeRecorderViewModel tested;

    public TimeRecorderViewModelTests()
    {
        tested = new(service);
    }

    [Fact]
    public async Task 記録開始の応答待ちでは操作できず開始に失敗しても一覧を確認できれば操作を再開できます()
    {
        var 応答 = service.DelayStartResponse();

        var 開始 = tested.RunUserOperationAsync(tested.StartTimeRecordAsync);
        tested.CanOperate.Should().BeFalse();
        応答.SetException(new InvalidOperationException());
        await 開始;

        tested.CanOperate.Should().BeTrue();
    }

    [Fact]
    public async Task 記録の開始失敗は応答待ちの間は表示せず失敗が確定してから知らせます()
    {
        var 応答 = service.DelayStartResponse();

        var 開始 = tested.RunUserOperationAsync(tested.StartTimeRecordAsync);
        tested.HasOperationFailed.Should().BeFalse();
        応答.SetException(new InvalidOperationException());
        await 開始;

        tested.HasOperationFailed.Should().BeTrue();
    }

    [Fact]
    public async Task 記録の開始に失敗した後で開始に成功しても以前の失敗表示は残ります()
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
    public async Task 記録の開始に失敗すると最新状態を確認できるまでは再取得を案内し確認後は開始の失敗を知らせます()
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
    public async Task 利用者が記録一覧を再取得して失敗すると操作の失敗として知らせます()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.HasOperationFailed.Should().BeTrue();
    }

    [Fact]
    public async Task 利用者が記録一覧を再取得して失敗すると記録の最新状態は未確認のままです()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task 利用者が記録一覧を再取得して失敗すると記録の開始と終了は引き続きできません()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.CanOperate.Should().BeFalse();
    }

    [Fact]
    public async Task 利用者が記録一覧を再取得して失敗すると通信中の状態が解除されます()
    {
        await 開始失敗後の再取得にも失敗させる();

        await tested.RunUserOperationAsync(tested.LoadAsync);

        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 記録一覧の初回取得に失敗するとその例外が呼び出し元に伝わります()
    {
        var 取得失敗 = new InvalidOperationException();
        一覧取得を失敗させる(取得失敗);

        (await Assert.ThrowsAsync<InvalidOperationException>(tested.LoadAsync)).Should().BeSameAs(取得失敗);
    }

    [Fact]
    public async Task 記録一覧の初回取得に失敗しても利用者の操作失敗としては表示しません()
    {
        一覧取得を失敗させる(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(tested.LoadAsync);

        tested.HasOperationFailed.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 同じ対象で記録中にもう一度開始しても追加の開始要求を送らず最初の記録だけが残ります(bool プロジェクトを指定する)
    {
        tested.SelectProject(プロジェクトを指定する ? プロジェクトA : null);
        await tested.LoadAsync();
        var 最初の記録 = service.StartedRecord;
        await tested.StartTimeRecordAsync();
        service.StartedRecord = new(別の記録ID, 終了時刻, null);

        await tested.StartTimeRecordAsync();

        service.StartCount.Should().Be(1);
        tested.Records.Should().Equal(最初の記録);
    }

    [Fact]
    public async Task 異なるプロジェクトで順に記録を開始すると両方の記録が開始順に一覧に残ります()
    {
        await プロジェクトAとBで記録を開始する();

        var 記録ID一覧 =
            from record in tested.Records
            select record.Id;
        記録ID一覧.Should().Equal(記録ID, 別の記録ID);
    }

    [Fact]
    public async Task 記録中に別のプロジェクトを選んで記録を開始するとそのプロジェクトも記録中になります()
    {
        await プロジェクトAとBで記録を開始する();

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 別のプロジェクトで記録を開始して元のプロジェクトに戻っても記録の終了は要求しません()
    {
        await プロジェクトAとBで記録を開始する();

        tested.SelectProject(プロジェクトA);

        service.EndedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task 別のプロジェクトで記録を開始して元のプロジェクトに戻ると元のプロジェクトは記録中のままです()
    {
        await プロジェクトAとBで記録を開始する();

        tested.SelectProject(プロジェクトA);

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task プロジェクト未指定で記録を開始して別のプロジェクトでも記録を開始すると未指定に戻っても記録中です()
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
    public async Task 記録中のプロジェクトから未指定に切り替えて戻ってもそのプロジェクトは記録中です()
    {
        await 未指定とプロジェクトの記録を開始する(プロジェクトA);
        tested.SelectProject(null);

        tested.SelectProject(プロジェクトA);

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 記録の開始応答を待つ間にプロジェクト未指定に切り替えても応答で確定した記録は一覧に加わります()
    {
        var (応答, 開始操作) = プロジェクトAの開始応答を待つ間に未指定へ切り替える();

        応答.SetResult(service.StartedRecord);
        await 開始操作;

        tested.Records.Should().ContainSingle().Which.Should().BeSameAs(service.StartedRecord);
    }

    [Fact]
    public async Task 記録の開始応答を待つ間にプロジェクト未指定に切り替えても開始を要求したプロジェクトだけが記録中になります()
    {
        var (応答, 開始操作) = プロジェクトAの開始応答を待つ間に未指定へ切り替える();

        応答.SetResult(service.StartedRecord);
        await 開始操作;

        tested.IsRecording.Should().BeFalse();
        tested.SelectProject(プロジェクトA);
        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 利用者がプロジェクトAの記録の終了応答を待つ間にBを選ぶとAだけが終了済みになりBの記録は引き続き終了できます()
    {
        await プロジェクトAとBで記録を開始する();
        var Aの終了済み記録 = new TimeRecord(記録ID, 開始時刻.AddMinutes(1), 終了時刻);
        var 応答 = service.DelayEndResponse();
        tested.SelectProject(プロジェクトA);

        var 終了操作 = tested.EndTimeRecordAsync();
        service.EndedIds.Should().Equal(記録ID);
        tested.SelectProject(プロジェクトB);
        応答.SetResult(Aの終了済み記録);
        await 終了操作;

        tested.Records.Should().Equal(Aの終了済み記録, service.StartedRecord);
        service.EndResponse = Task.FromResult(service.StartedRecord.End(終了時刻.AddMinutes(1)));
        await tested.EndTimeRecordAsync();
        var Bの終了操作直後の要求 = service.EndedIds.ToArray();

        tested.SelectProject(プロジェクトA);
        service.EndResponse = 応答.Task;
        await tested.EndTimeRecordAsync();
        Bの終了操作直後の要求.Should().Equal(記録ID, 別の記録ID);
        service.EndedIds.Should().Equal(Bの終了操作直後の要求);
    }

    [Fact]
    public async Task 終了通信の失敗を待つ間に別のプロジェクトを選んでも再取得で確認した終了済み記録の終了は再要求しません()
    {
        var Aの記録 = service.StartedRecord;
        await プロジェクトAとBで記録を開始する();
        var Bの終了済み記録 = service.StartedRecord.End(終了時刻.AddMinutes(1));
        service.Records = [Aの記録, Bの終了済み記録];
        var 応答 = service.DelayEndResponse();

        var Bの終了操作 = tested.EndTimeRecordAsync();
        service.EndedIds.Should().Equal(別の記録ID);
        tested.SelectProject(プロジェクトA);
        応答.SetException(new HttpRequestException());
        await Assert.ThrowsAsync<HttpRequestException>(() => Bの終了操作);

        tested.Records.Should().Equal(Aの記録, Bの終了済み記録);

        tested.SelectProject(プロジェクトB);
        service.EndResponse = Task.FromResult(Bの終了済み記録);
        await tested.EndTimeRecordAsync();

        tested.SelectProject(プロジェクトA);
        service.EndResponse = null;
        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(別の記録ID, 記録ID);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task プロジェクト指定と未指定の記録が同時に進んでいるときは選択中の記録IDで終了を要求します(bool プロジェクトを終了する)
    {
        var (終了する記録ID, _, _) = await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(終了する記録ID);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task プロジェクト指定と未指定のうち選択中の記録を終了するとその対象は記録中ではなくなります(bool プロジェクトを終了する)
    {
        await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task プロジェクト指定と未指定の一方の記録を終了しても他方に切り替えると記録中のままです(bool プロジェクトを終了する)
    {
        var (_, 残るプロジェクト, _) = await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();
        tested.SelectProject(残るプロジェクト);

        tested.IsRecording.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task プロジェクト指定と未指定の一方の記録を終了しても一覧にある他方の記録の終了時刻は空のままです(bool プロジェクトを終了する)
    {
        var (_, _, 残る記録ID) = await 未指定とプロジェクトの記録から終了対象を選ぶ(プロジェクトを終了する);

        await tested.EndTimeRecordAsync();

        tested.Records.Single(it => it.Id == 残る記録ID).EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task 記録の開始応答を待つ間は開始や終了を操作しても追加の要求を送りません()
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
    public async Task 記録の開始に成功すると開始した記録の終了を要求できます()
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
    public async Task 記録の開始に失敗しても一覧に記録がなければ再び開始を要求できます()
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
    public async Task 記録の終了応答を待つ間は開始や終了を操作しても追加の要求を送りません()
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
    public async Task 記録の終了に成功すると新しい記録の開始を要求できます()
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
    public async Task 記録の終了に失敗しても一覧で記録中と確認できれば同じ記録の終了を再び要求できます()
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
    public async Task 記録の開始や終了の通信に失敗しても一覧の再取得に成功すれば元の通信例外が呼び出し元に伝わります(bool 終了操作)
    {
        var 通信失敗 = new HttpRequestException();
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, 通信失敗);

        (await Assert.ThrowsAsync<HttpRequestException>(操作)).Should().BeSameAs(通信失敗);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録の開始や終了の通信に失敗すると記録一覧を一度だけ再取得します(bool 終了操作)
    {
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        service.ListCount.Should().Be(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録の開始や終了の通信に失敗すると再取得した記録で一覧を更新します(bool 終了操作)
    {
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.Records.Should().Equal(service.Records);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録の開始や終了の通信に失敗すると再取得した一覧をもとに記録中かどうかを判定します(bool 終了操作)
    {
        var 操作 = await 再取得で結果を確認できる通信失敗を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.IsRecording.Should().Be(!終了操作);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録の開始や終了に失敗して一覧も再取得できないと再取得の例外が呼び出し元に伝わります(bool 終了操作)
    {
        var 再取得失敗 = new HttpRequestException();
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, 再取得失敗);

        (await Assert.ThrowsAsync<HttpRequestException>(操作)).Should().BeSameAs(再取得失敗);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録の開始や終了に失敗して一覧も再取得できないと直前の記録一覧を残します(bool 終了操作)
    {
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, new HttpRequestException());
        IEnumerable<TimeRecord> 直前の記録 = 終了操作 ? [service.StartedRecord] : service.Records;

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.Records.Should().Equal(直前の記録);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録の開始や終了に失敗して一覧も再取得できないと記録の最新状態は未確認になります(bool 終了操作)
    {
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録の開始や終了に失敗して一覧も再取得できないと通信中の状態が解除されます(bool 終了操作)
    {
        var 操作 = await 再取得にも失敗する記録操作を用意する(終了操作, new HttpRequestException());

        await Assert.ThrowsAsync<HttpRequestException>(操作);

        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 記録の開始と一覧の再取得に失敗した後で古い初回一覧が届いても記録の最新状態は未確認のままです()
    {
        var 初期一覧の応答 = service.DelayListResponse();
        var 初期一覧の読み込み = tested.LoadAsync();
        await 開始失敗後の再取得にも失敗させる();

        初期一覧の応答.SetResult([]);
        await 初期一覧の読み込み;

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task 記録の開始に失敗した後で一覧を確認できるまでは追加の開始や終了を要求しません()
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
    public async Task 記録の終了に失敗した後で一覧を確認できるまでは追加の開始や終了を要求しません()
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
    public async Task 記録一覧の手動再取得に成功すると取得した記録で一覧を更新します(bool 記録中)
    {
        await 手動再取得で返す記録を用意する(記録中);

        await tested.LoadAsync();

        tested.Records.Should().Equal(記録中 ? service.StartedRecord : service.EndedRecord);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録一覧を手動で再取得できれば記録の最新状態は確認済みになります(bool 記録中)
    {
        await 手動再取得で返す記録を用意する(記録中);

        await tested.LoadAsync();

        tested.IsStateUnconfirmed.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録一覧を手動で再取得すると取得した記録が未終了の場合だけ記録中になります(bool 記録中)
    {
        await 手動再取得で返す記録を用意する(記録中);

        await tested.LoadAsync();

        tested.IsRecording.Should().Be(記録中);
    }

    [Fact]
    public async Task 記録一覧を手動で再取得して未終了の記録を確認するとその記録を終了できます()
    {
        await 手動再取得で返す記録を用意する(記録中: true);
        await tested.LoadAsync();

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 記録一覧を手動で再取得して終了済みの記録を確認すると新しい記録を開始できます()
    {
        await 手動再取得で返す記録を用意する(記録中: false);
        await tested.LoadAsync();

        await tested.StartTimeRecordAsync();

        service.StartCount.Should().Be(2);
    }

    [Fact]
    public async Task 記録の開始に失敗して自動でも手動でも一覧を再取得できないと直前の一覧が残ります()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        tested.Records.Should().Equal(service.Records);
    }

    [Fact]
    public async Task 記録の開始に失敗して自動でも手動でも一覧を再取得できないと最新状態は未確認のままです()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        tested.IsStateUnconfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task 記録の開始に失敗して自動でも手動でも一覧を再取得できないと通信中の状態が解除されます()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        tested.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task 記録の開始と一覧の自動再取得に失敗しても手動で一覧を再要求できます()
    {
        await 終了済みの履歴がある状態で開始と再取得を失敗させる();

        await Assert.ThrowsAsync<HttpRequestException>(() => tested.LoadAsync());

        service.ListCount.Should().Be(3);
    }

    [Fact]
    public async Task 記録一覧の手動再取得の応答を待つ間は通信中になります()
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
    public async Task 記録一覧の手動再取得の応答を待つ間にもう一度再取得しても一覧を重ねて要求しません()
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
    public async Task 記録一覧を手動で再取得している間は記録の開始や終了を要求しません()
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
    public void 初期状態の記録一覧は空です()
    {
        tested.Records.Should().BeEmpty();
    }

    [Fact]
    public void 初期状態の操作欄には作業記録を表示しません()
    {
        tested.DisplayedRecord.Should().BeNull();
    }

    [Fact]
    public void 記録画面の初期状態では記録中の作業はありません()
    {
        tested.IsRecording.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録一覧にはサーバーから読み込んだ順に作業記録が並びます(bool 記録中の項目を含む)
    {
        読み込む記録一覧を用意する(記録中の項目を含む);

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.Records);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 読み込んだ一覧に未終了の記録が含まれる場合だけ記録中になります(bool 記録中の項目を含む)
    {
        読み込む記録一覧を用意する(記録中の項目を含む);

        await tested.LoadAsync();

        tested.IsRecording.Should().Be(記録中の項目を含む);
    }

    [Fact]
    public async Task 一覧に記録中の作業があっても読み込んだだけでは操作欄に記録を表示しません()
    {
        読み込む記録一覧を用意する(記録中の項目を含む: true);

        await tested.LoadAsync();

        tested.DisplayedRecord.Should().BeNull();
    }

    [Fact]
    public async Task 記録の開始に成功すると確定した記録が一覧の末尾に加わります()
    {
        await 終了済みの履歴を読み込む();

        await tested.StartTimeRecordAsync();

        tested.Records.Should().Equal(service.Records[0], service.StartedRecord);
    }

    [Fact]
    public async Task 記録の開始に成功すると確定した記録が操作欄に表示されます()
    {
        await 終了済みの履歴を読み込む();

        await tested.StartTimeRecordAsync();

        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
    }

    [Fact]
    public async Task 記録の開始に成功すると選択中の作業は記録中になります()
    {
        await 終了済みの履歴を読み込む();

        await tested.StartTimeRecordAsync();

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 記録の開始後に古い初回一覧が届いても過去の記録の後に新しく開始した記録が並びます()
    {
        await 記録開始後に古い初回一覧を受け取る();

        tested.Records.Should().Equal(過去の記録, service.StartedRecord);
    }

    [Fact]
    public async Task 記録の開始後に古い初回一覧が届いても操作欄には新しく開始した記録が残ります()
    {
        await 記録開始後に古い初回一覧を受け取る();

        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
    }

    [Fact]
    public async Task 記録の開始後に古い初回一覧が届いてもその記録を終了するまで記録中のままです()
    {
        await 記録開始後に古い初回一覧を受け取る();

        tested.IsRecording.Should().BeTrue();

        await tested.EndTimeRecordAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task 記録の開始後に古い初回一覧が届いても新しく開始した記録のIDで終了を要求します()
    {
        await 記録開始後に古い初回一覧を受け取る();

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Fact]
    public async Task 記録の開始後に古い初回一覧が届いても新しい記録を終了した一覧には過去の記録が残ります()
    {
        await 記録開始後に古い初回一覧を受け取る();

        await tested.EndTimeRecordAsync();

        tested.Records.Should().Equal(過去の記録, service.EndedRecord);
    }

    [Fact]
    public async Task 一覧の取得中に終了した記録が応答では未終了でも同じIDの記録は終了済みの一件だけが残ります()
    {
        await 記録の更新と別の記録の開始後に古い一覧を受け取る();

        var 同じIDの記録 =
            from record in tested.Records
            where record.Id == 記録ID
            select record;
        同じIDの記録.Should().Equal(service.EndedRecord);
    }

    [Fact]
    public async Task 一覧の取得中に開始した記録が応答に含まれなければ一覧の末尾に加えます()
    {
        await 記録の更新と別の記録の開始後に古い一覧を受け取る();

        var 記録ID一覧 =
            from record in tested.Records
            select record.Id;
        記録ID一覧.Should().Equal(記録ID, 別の記録ID);
        tested.Records[^1].Should().Be(service.StartedRecord);
    }

    [Fact]
    public async Task 一覧の取得中に開始した複数の記録が応答に含まれなくても各記録の最新状態が開始順に残ります()
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
    public async Task Aで記録を開始して未開始のBを選んだ後に空の初回一覧が届いてもAに戻ってその記録だけを終了できます()
    {
        var 応答 = service.DelayListResponse();
        var 読み込み = tested.LoadAsync();
        tested.SelectProject(プロジェクトA);
        await tested.StartTimeRecordAsync();
        tested.SelectProject(プロジェクトB);

        応答.SetResult([]);
        await 読み込み;

        tested.IsRecording.Should().BeFalse();
        await tested.EndTimeRecordAsync();
        service.EndedIds.Should().BeEmpty();

        tested.SelectProject(プロジェクトA);
        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
        tested.Records.Should().Equal(service.EndedRecord);
    }

    [Fact]
    public async Task 一覧の取得中に記録を開始すると応答の先頭に別の未終了の記録があっても新しく開始した記録から終了します()
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
    public async Task 記録の開始後に一覧を再読み込みするとサーバーの最新の一覧に置き換わります()
    {
        await 履歴がある状態で記録を開始する();
        service.Records = [service.EndedRecord];

        await tested.LoadAsync();

        tested.Records.Should().Equal(service.EndedRecord);
    }

    [Fact]
    public async Task 開始した記録が再読み込みした一覧で終了済みなら選択中の作業は記録中ではなくなります()
    {
        await 履歴がある状態で記録を開始する();
        service.Records = [service.EndedRecord];

        await tested.LoadAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録を終了すると記録サービスに終了対象の記録IDを渡します(bool 一覧から読み込む)
    {
        await 終了対象の記録を用意する(一覧から読み込む);

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(記録ID);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録を終了すると一覧の履歴を残して終了対象の記録だけを更新します(bool 一覧から読み込む)
    {
        await 終了対象の記録を用意する(一覧から読み込む);

        await tested.EndTimeRecordAsync();

        tested.Records.Should().Equal(service.Records[0], service.EndedRecord);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 記録を終了すると選択中の作業は記録中ではなくなります(bool 一覧から読み込む)
    {
        await 終了対象の記録を用意する(一覧から読み込む);

        await tested.EndTimeRecordAsync();

        tested.IsRecording.Should().BeFalse();
    }

    [Fact]
    public async Task この画面で開始した記録を終了すると操作欄に終了済みの記録が残ります()
    {
        await 履歴がある状態で記録を開始する();

        await tested.EndTimeRecordAsync();

        tested.DisplayedRecord.Should().BeSameAs(service.EndedRecord);
    }

    [Fact]
    public async Task 記録を終了して新しく開始すると新しい記録が終了済みの履歴の後に加わります()
    {
        await 記録を終了して次に別の記録を開始できるようにする();

        await tested.StartTimeRecordAsync();

        tested.Records.Should().Equal(service.EndedRecord, service.StartedRecord);
    }

    [Fact]
    public async Task 記録を終了して新しく開始すると操作欄には新しい未終了の記録が表示されます()
    {
        await 記録を終了して次に別の記録を開始できるようにする();

        await tested.StartTimeRecordAsync();

        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
        tested.DisplayedRecord.EndedAt.Should().BeNull();
    }

    [Fact]
    public async Task 記録を終了して新しく開始すると選択中の作業は再び記録中になります()
    {
        await 記録を終了して次に別の記録を開始できるようにする();

        await tested.StartTimeRecordAsync();

        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 一覧から読み込んだ記録を終了しても操作欄にはその終了結果を表示しません()
    {
        await 終了対象の記録を用意する(一覧から読み込む: true);

        await tested.EndTimeRecordAsync();

        tested.DisplayedRecord.Should().BeNull();
    }

    [Fact]
    public async Task 記録中の作業がなければ記録サービスに終了を要求しません()
    {
        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task 初期状態で記録の終了を要求しても一覧と操作欄は空のままで記録中にもなりません()
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
        tested.SelectProject(プロジェクトB);
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
