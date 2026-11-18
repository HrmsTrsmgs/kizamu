using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecordRefreshTimerTests : IDisposable
{
    readonly FakeTimeProvider clock = new();
    readonly TimeRecordRefreshTimer tested;
    int 更新回数;

    public TimeRecordRefreshTimerTests() => tested = new(clock, () => 更新回数++);

    [Fact]
    public void 記録中に設定し直すとその一秒後から一秒ごとに一度だけ更新する()
    {
        tested.ScheduleRefresh(null);
        clock.Advance(TimeSpan.FromMilliseconds(500));

        tested.ScheduleRefresh(null);

        clock.Advance(TimeSpan.FromMilliseconds(500));
        更新回数.Should().Be(0);
        clock.Advance(TimeSpan.FromMilliseconds(500));
        更新回数.Should().Be(1);
        clock.Advance(TimeSpan.FromSeconds(1));
        更新回数.Should().Be(2);
    }

    [Fact]
    public void 記録を終了すると定期更新が止まる()
    {
        tested.ScheduleRefresh(null);

        tested.ScheduleRefresh(clock.GetUtcNow());
        clock.Advance(TimeSpan.FromSeconds(1));

        更新回数.Should().Be(0);
    }

    [Fact]
    public void 記録中に破棄すると定期更新が止まる()
    {
        tested.ScheduleRefresh(null);

        tested.Dispose();
        clock.Advance(TimeSpan.FromSeconds(1));

        更新回数.Should().Be(0);
    }

    public void Dispose() => tested.Dispose();
}
