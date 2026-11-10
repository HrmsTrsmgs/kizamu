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

    [Fact]
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task 一方の記録を終了しても他方は記録中のままになる(bool プロジェクトを終了する)
    {
        var プロジェクトA = new Project("プロジェクトA");
        var (終了するプロジェクト, 終了する記録ID, 残るプロジェクト, 残る記録ID) =
            プロジェクトを終了する
                ? (プロジェクトA, 別の記録ID, (Project?)null, 記録ID)
                : ((Project?)null, 記録ID, プロジェクトA, 別の記録ID);
        var service = new StubTimeRecordService(
            new(記録ID, 開始時刻, null), new(終了する記録ID, 開始時刻, 終了時刻));
        var tested = new TimeRecorderViewModel(service);
        await 未指定とプロジェクトAの記録を開始する();
        tested.SelectProject(終了するプロジェクト);

        await tested.EndTimeRecordAsync();

        service.EndedIds.Should().Equal(終了する記録ID);
        tested.IsRecording.Should().BeFalse();
        tested.SelectProject(残るプロジェクト);
        tested.IsRecording.Should().BeTrue();
        tested.Records.Single(it => it.Id == 残る記録ID).EndedAt.Should().BeNull();

        async Task 未指定とプロジェクトAの記録を開始する()
        {
            tested.SelectProject(null);
            await tested.StartTimeRecordAsync();
            tested.SelectProject(プロジェクトA);
            service.StartedRecord = new(別の記録ID, 開始時刻, null);
            await tested.StartTimeRecordAsync();
        }
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
        service.Records = [new(別の記録ID, 開始時刻, 終了時刻)];
        await tested.LoadAsync();

        await tested.StartTimeRecordAsync();

        tested.Records.Should().Equal(service.Records[0], service.StartedRecord);
        tested.DisplayedRecord.Should().BeSameAs(service.StartedRecord);
        tested.IsRecording.Should().BeTrue();
    }

    [Fact]
    public async Task 終了すると開始した記録だけが更新され操作結果が残る()
    {
        service.Records = [new(別の記録ID, 開始時刻, 終了時刻)];
        await tested.LoadAsync();
        await tested.StartTimeRecordAsync();

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
}
