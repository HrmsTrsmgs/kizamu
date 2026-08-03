using FluentAssertions;
using Kizamu.Infrastructure.TimeRecords;

namespace Kizamu.Infrastructure.Tests.TimeRecords;

public sealed class InMemoryTimeRecordStateTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void 開始時刻を保存すると取得できます()
    {
        var timeRecordState = new InMemoryTimeRecordState
        {
            StartedAt = 開始時刻,
        };

        timeRecordState.StartedAt.Should().Be(開始時刻);
    }
}
