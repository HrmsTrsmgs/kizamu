using AwesomeAssertions;
using Kizamu.Domain;

namespace Kizamu.Domain.Tests;

public sealed class TimeRecordTests
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 終了時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);

    readonly TimeRecord tested = TimeRecord.Start(開始時刻);

    public static TheoryData<DateTimeOffset?> 復元する終了時刻 => [(DateTimeOffset?)null, 終了時刻];

    [Theory]
    [MemberData(nameof(復元する終了時刻))]
    public void 既存の記録の識別子と時刻から状態を復元できます(DateTimeOffset? endedAt)
    {
        var 復元した記録 = new TimeRecord(記録ID, 開始時刻, endedAt);

        復元した記録.Id.Should().Be(記録ID);
        復元した記録.StartedAt.Should().Be(開始時刻);
        復元した記録.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void 記録を開始すると空でない識別子が割り当てられます()
    {
        tested.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void 記録を開始すると指定した時刻が開始時刻になります()
    {
        tested.StartedAt.Should().Be(開始時刻);
    }

    [Fact(Skip = "現在のEndは元の記録を更新するため。元の記録を開始状態に保ち、終了済みのTimeRecordを返すようになったら解除する。")]
    public void 記録を終了すると元の記録は開始状態のまま終了済みの記録が返ります()
    {
        TimeRecord 終了した記録 = ((dynamic)tested).End(終了時刻);

        終了した記録.Id.Should().Be(tested.Id);
        終了した記録.StartedAt.Should().Be(開始時刻);
        終了した記録.EndedAt.Should().Be(終了時刻);
        tested.EndedAt.Should().BeNull();
    }
}
