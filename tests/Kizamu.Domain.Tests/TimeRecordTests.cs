using FluentAssertions;
using Kizamu.Domain;

namespace Kizamu.Domain.Tests;

public sealed class TimeRecordTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void 記録を開始すると空でない識別子が割り当てられます()
    {
        var timeRecord = TimeRecord.Start(開始時刻);

        timeRecord.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void 記録を開始すると指定した時刻が開始時刻になります()
    {
        var timeRecord = TimeRecord.Start(開始時刻);

        timeRecord.StartedAt.Should().Be(開始時刻);
    }
}
