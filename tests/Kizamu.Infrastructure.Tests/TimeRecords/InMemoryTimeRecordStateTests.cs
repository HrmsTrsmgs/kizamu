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
        var timeRecordState = new InMemoryTimeRecordState
        {
            Current = timeRecord,
        };

        timeRecordState.Current.Should().BeSameAs(timeRecord);
    }
}
