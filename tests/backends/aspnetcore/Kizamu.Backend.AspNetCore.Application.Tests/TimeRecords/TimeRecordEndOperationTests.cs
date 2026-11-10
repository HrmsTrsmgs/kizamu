using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class TimeRecordEndOperationTests
{
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
        var tested = new TimeRecordEndOperation(new FakeTimeProvider(endedAt), state);

        tested.Execute(timeRecord.Id).Should().Be(new EndTimeRecordResult(timeRecord.Id, startedAt, endedAt));
        timeRecord.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void 複数の記録から指定した記録だけを終了する()
    {
        var 最初の記録 = TimeRecord.Start(早い時刻);
        var 次の記録 = TimeRecord.Start(中間の時刻);
        var state = new FakeTimeRecordState();
        state.Add(最初の記録);
        state.Add(次の記録);
        var tested = new TimeRecordEndOperation(new FakeTimeProvider(遅い時刻), state);

        tested.Execute(最初の記録.Id).Should().Be(new EndTimeRecordResult(最初の記録.Id, 早い時刻, 遅い時刻));
        最初の記録.EndedAt.Should().Be(遅い時刻);
        次の記録.EndedAt.Should().BeNull();
    }

    [Fact]
    public void 終了済みの記録を再度指定すると終了時刻を更新し他の記録は変化しない()
    {
        var 最初の記録 = TimeRecord.Start(早い時刻);
        最初の記録.End(中間の時刻);
        var 次の記録 = TimeRecord.Start(中間の時刻);
        var state = new FakeTimeRecordState();
        state.Add(最初の記録);
        state.Add(次の記録);
        var tested = new TimeRecordEndOperation(new FakeTimeProvider(遅い時刻), state);

        tested.Execute(最初の記録.Id).Should().Be(new EndTimeRecordResult(最初の記録.Id, 早い時刻, 遅い時刻));
        最初の記録.EndedAt.Should().Be(遅い時刻);
        次の記録.EndedAt.Should().BeNull();
    }

    [Fact]
    public void 異なる識別子で終了を要求しても現在の記録は終了しない()
    {
        var timeRecord = TimeRecord.Start(早い時刻);
        var state = new FakeTimeRecordState();
        state.Add(timeRecord);
        var tested = new TimeRecordEndOperation(new FakeTimeProvider(遅い時刻), state);

        // 対象が異なる場合の戻り値や例外は未確定のため、記録への影響だけを確認する。
        Record.Exception(() => tested.Execute(Guid.NewGuid()));

        timeRecord.EndedAt.Should().BeNull();
    }
}
