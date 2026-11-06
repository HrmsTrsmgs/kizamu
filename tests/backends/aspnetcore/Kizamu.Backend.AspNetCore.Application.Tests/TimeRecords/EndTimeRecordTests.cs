using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class EndTimeRecordTests
{
    static readonly Guid 識別子 = new("019cde5b-4c00-7000-8000-000000000001");
    static readonly DateTimeOffset 早い時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 中間の時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 遅い時刻 = new(2026, 8, 1, 11, 0, 0, TimeSpan.Zero);

    public static TheoryData<DateTimeOffset, DateTimeOffset> 記録時刻の例 =>
    [
        (早い時刻, 中間の時刻),
        (中間の時刻, 遅い時刻),
    ];

    [Theory]
    [MemberData(nameof(記録時刻の例))]
    public void 記録を終了すると指定した識別子と保存された開始時刻と現在の終了時刻を返し状態を更新する(
        DateTimeOffset startedAt,
        DateTimeOffset endedAt)
    {
        var timeRecord = TimeRecord.Start(startedAt);
        var state = new FakeTimeRecordState();
        state.Add(timeRecord);
        var tested = new EndTimeRecord(new FakeTimeProvider(endedAt), state);

        tested.Execute(識別子).Should().Be(new EndTimeRecordResult(識別子, startedAt, endedAt));
        timeRecord.EndedAt.Should().Be(endedAt);
    }
}
