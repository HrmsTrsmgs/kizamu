using AwesomeAssertions;
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
