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
    readonly FakeTimeProvider timeProvider = new();
    readonly FakeTimeRecordState state = new();
    readonly TimeRecordEndOperation tested;

    public TimeRecordEndOperationTests() => tested = new(timeProvider, state);

    public static TheoryData<DateTimeOffset, DateTimeOffset?, DateTimeOffset> 終了前の記録と要求時刻 =>
    [
        (早い時刻, null, 中間の時刻),
        (中間の時刻, null, 遅い時刻),
        (早い時刻, 中間の時刻, 遅い時刻),
    ];

    [Theory]
    [MemberData(nameof(終了前の記録と要求時刻))]
    public void 先に保存した記録を終了するとその識別子と開始時刻と今回の終了時刻が返ります(
        DateTimeOffset startedAt, DateTimeOffset? previousEndedAt, DateTimeOffset requestedAt)
    {
        var (最初の記録, _) = 二件の記録を保存する(startedAt, previousEndedAt);
        timeProvider.SetUtcNow(requestedAt);

        tested.Execute(最初の記録.Id).Should().Be(new EndTimeRecordResult(最初の記録.Id, startedAt, requestedAt));
    }

    [Theory]
    [MemberData(nameof(終了前の記録と要求時刻))]
    public void 先に保存した記録を終了するとその記録だけに今回の終了時刻が保存されます(
        DateTimeOffset startedAt, DateTimeOffset? previousEndedAt, DateTimeOffset requestedAt)
    {
        var (最初の記録, 次の記録) = 二件の記録を保存する(startedAt, previousEndedAt);
        timeProvider.SetUtcNow(requestedAt);

        tested.Execute(最初の記録.Id);

        state.Find(最初の記録.Id).Should().Be(new TimeRecord(最初の記録.Id, startedAt, requestedAt));
        state.Find(次の記録.Id).Should().Be(次の記録);
    }

    [Fact]
    public void 存在しない識別子で終了を要求しても保存済みの記録は変わりません()
    {
        var timeRecord = TimeRecord.Start(早い時刻);
        state.Add(timeRecord);
        timeProvider.SetUtcNow(遅い時刻);

        // 対象が異なる場合の戻り値や例外は未確定のため、記録への影響だけを確認する。
        Record.Exception(() => tested.Execute(Guid.NewGuid()));

        state.Find(timeRecord.Id).Should().Be(timeRecord);
    }

    (TimeRecord 最初の記録, TimeRecord 次の記録) 二件の記録を保存する(DateTimeOffset startedAt, DateTimeOffset? endedAt)
    {
        var 最初の記録 = new TimeRecord(Guid.NewGuid(), startedAt, endedAt);
        var 次の記録 = TimeRecord.Start(startedAt.AddMinutes(15));
        state.Add(最初の記録);
        state.Add(次の記録);
        return (最初の記録, 次の記録);
    }
}
