using AwesomeAssertions;
using Kizamu.Domain;

namespace Kizamu.Domain.Tests;

public sealed class TimeRecordTests
{
    static readonly Guid 記録ID = Guid.Parse("019cde5b-4c00-7000-8000-000000000001");
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 終了時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);

    public static TheoryData<DateTimeOffset?> 復元する終了時刻 => [(DateTimeOffset?)null, 終了時刻];

    [Theory]
    [MemberData(nameof(復元する終了時刻))]
    public void 既存の記録の識別子と時刻から状態を復元できます(DateTimeOffset? endedAt)
    {
        var tested = new TimeRecord(記録ID, 開始時刻, endedAt);

        tested.Id.Should().Be(記録ID);
        tested.StartedAt.Should().Be(開始時刻);
        tested.EndedAt.Should().Be(endedAt);
    }

    [Fact]
    public void 記録を開始すると空でない識別子が割り当てられます()
    {
        var tested = TimeRecord.Start(開始時刻);

        tested.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void 記録を開始すると指定した時刻が開始時刻になります()
    {
        var tested = TimeRecord.Start(開始時刻);

        tested.StartedAt.Should().Be(開始時刻);
    }

    [Fact]
    public void 記録を終了すると指定した時刻が終了時刻になります()
    {
        var tested = TimeRecord.Start(開始時刻);

        tested.End(終了時刻);

        tested.EndedAt.Should().Be(終了時刻);
    }
}
