using FluentAssertions;
using Kizamu.Domain;
using Kizamu.Infrastructure.TimeRecords;

namespace Kizamu.Infrastructure.Tests.TimeRecords;

public sealed class InMemoryTimeRecordStateTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void 作業記録を保存すると同じ記録を取得できます()
    {
        var timeRecord = TimeRecord.Start(開始時刻);
        var tested = new InMemoryTimeRecordState
        {
            Current = timeRecord,
        };

        tested.Current.Should().BeSameAs(timeRecord);
    }

    [Fact]
    public void 作業記録を追加していない場合は空の一覧を取得できます()
    {
        var tested = new InMemoryTimeRecordState();

        tested.All.Should().BeEmpty();
    }

    [Fact]
    public void 作業記録を一件追加すると追加した記録を取得できます()
    {
        var 追加する記録 = TimeRecord.Start(開始時刻);
        var tested = new InMemoryTimeRecordState();

        tested.Add(追加する記録);

        tested.All.Should().Equal(追加する記録);
    }
}
