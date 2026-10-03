using AwesomeAssertions;
using Kizamu.Domain;

namespace Kizamu.Domain.Tests;

public sealed class TimeRecordTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 終了時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);

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
