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

    public static TheoryData<DateTimeOffset, DateTimeOffset> 記録時刻の例 =>
    [
        (早い時刻, 中間の時刻),
        (中間の時刻, 遅い時刻),
    ];

    [Theory]
    [MemberData(nameof(記録時刻の例))]
    public void 記録を終了すると指定した識別子と保存された開始時刻と現在の終了時刻を返す(
        DateTimeOffset startedAt, DateTimeOffset endedAt)
    {
        var timeRecord = 記録を開始して保存する(startedAt);
        timeProvider.SetUtcNow(endedAt);

        tested.Execute(timeRecord.Id).Should().Be(new EndTimeRecordResult(timeRecord.Id, startedAt, endedAt));
    }

    [Theory]
    [MemberData(nameof(記録時刻の例))]
    public void 記録を終了すると保存された記録の終了時刻を現在時刻に更新する(
        DateTimeOffset startedAt, DateTimeOffset endedAt)
    {
        var timeRecord = 記録を開始して保存する(startedAt);
        timeProvider.SetUtcNow(endedAt);

        tested.Execute(timeRecord.Id);

        timeRecord.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void 複数の記録があるときは指定した記録の終了結果を返す()
    {
        var (最初の記録, _) = 二件の記録を開始して保存する();
        timeProvider.SetUtcNow(遅い時刻);

        tested.Execute(最初の記録.Id).Should().Be(new EndTimeRecordResult(最初の記録.Id, 早い時刻, 遅い時刻));
    }

    [Fact]
    public void 複数の記録から指定した記録だけを終了する()
    {
        var (最初の記録, 次の記録) = 二件の記録を開始して保存する();
        timeProvider.SetUtcNow(遅い時刻);

        tested.Execute(最初の記録.Id);

        最初の記録.EndedAt.Should().Be(遅い時刻);
        次の記録.EndedAt.Should().BeNull();
    }

    [Fact]
    public void 終了済みの記録を再度指定すると更新した終了時刻を返す()
    {
        var (最初の記録, _) = 終了済みと記録中の二件を保存する();
        timeProvider.SetUtcNow(遅い時刻);

        tested.Execute(最初の記録.Id).Should().Be(new EndTimeRecordResult(最初の記録.Id, 早い時刻, 遅い時刻));
    }

    [Fact]
    public void 終了済みの記録を再度指定するとその記録だけの終了時刻を更新する()
    {
        var (最初の記録, 次の記録) = 終了済みと記録中の二件を保存する();
        timeProvider.SetUtcNow(遅い時刻);

        tested.Execute(最初の記録.Id);

        最初の記録.EndedAt.Should().Be(遅い時刻);
        次の記録.EndedAt.Should().BeNull();
    }

    [Fact]
    public void 異なる識別子で終了を要求しても現在の記録は終了しない()
    {
        var timeRecord = 記録を開始して保存する(早い時刻);
        timeProvider.SetUtcNow(遅い時刻);

        // 対象が異なる場合の戻り値や例外は未確定のため、記録への影響だけを確認する。
        Record.Exception(() => tested.Execute(Guid.NewGuid()));

        timeRecord.EndedAt.Should().BeNull();
    }

    (TimeRecord 最初の記録, TimeRecord 次の記録) 二件の記録を開始して保存する() =>
        (記録を開始して保存する(早い時刻), 記録を開始して保存する(中間の時刻));

    (TimeRecord 終了済み, TimeRecord 記録中) 終了済みと記録中の二件を保存する()
    {
        var 終了済み = 記録を開始して保存する(早い時刻);
        終了済み.End(中間の時刻);
        return (終了済み, 記録を開始して保存する(中間の時刻));
    }

    TimeRecord 記録を開始して保存する(DateTimeOffset startedAt)
    {
        var record = TimeRecord.Start(startedAt);
        state.Add(record);
        return record;
    }
}
