using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecordRefreshTimerTests : IDisposable
{
    readonly FakeTimeProvider clock = new();
    readonly TimeRecordRefreshTimer tested;
    int 更新回数;

    public TimeRecordRefreshTimerTests()
    {
        tested = new(clock, () => 更新回数++);
        tested.ScheduleRefresh(null);
    }

    [Fact]
    public void 記録中に更新タイマーを設定し直すと一秒後から一秒に一度だけ更新を通知します()
    {
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
    public void 記録の終了時刻を設定すると更新タイマーの定期通知が止まります()
    {
        tested.ScheduleRefresh(clock.GetUtcNow());
        clock.Advance(TimeSpan.FromSeconds(1));

        更新回数.Should().Be(0);
    }

    [Fact]
    public void 記録中に更新タイマーを破棄すると定期通知が止まります()
    {
        tested.Dispose();
        clock.Advance(TimeSpan.FromSeconds(1));

        更新回数.Should().Be(0);
    }

    public void Dispose() => tested.Dispose();
}
