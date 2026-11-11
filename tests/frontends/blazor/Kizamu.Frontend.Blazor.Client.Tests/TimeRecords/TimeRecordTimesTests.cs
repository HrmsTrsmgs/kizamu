using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecordTimesTests : IDisposable
{
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    readonly BunitContext context = new();
    readonly FakeTimeProvider clock = new(開始時刻);

    public TimeRecordTimesTests() => context.Services.AddSingleton<TimeProvider>(clock);

    [Fact]
    public void 時刻要素に開始時刻と終了時刻の表示と元の日時を設定します()
    {
        var tested = 表示する(開始時刻.AddMinutes(90));
        var 時刻要素 = tested.FindAll("time");

        時刻要素.Select(it => it.TextContent).Should().Equal("2026/08/01 18:00", "2026/08/01 19:30");
        時刻要素.Select(it => it.GetAttribute("datetime")).Should().Equal(
            "2026-08-01T09:00:00.0000000+00:00", "2026-08-01T10:30:00.0000000+00:00");
    }

    [Fact]
    public async Task 記録中の作業時間が次の整数分になると表示を更新します()
    {
        clock.SetUtcNow(開始時刻.AddMinutes(90).AddSeconds(59));
        var tested = 表示する(null);

        await tested.InvokeAsync(() => clock.Advance(TimeSpan.FromSeconds(1)));

        tested.Find(".time-record-duration-value").TextContent.Should().Be("1時間31分");
    }

    [Fact]
    public async Task 終了済みの作業時間は現在時刻が進んでも変わりません()
    {
        var tested = 表示する(開始時刻.AddMinutes(90));

        await tested.InvokeAsync(() => clock.Advance(TimeSpan.FromHours(1)));

        tested.Find(".time-record-duration-value").TextContent.Should().Be("1時間30分");
    }

    [Fact]
    public async Task 記録を終了すると定期的な再描画を停止します()
    {
        var tested = 表示する(null);
        tested.Render(it => it.Add(it => it.EndedAt, 開始時刻.AddMinutes(90)));
        var 終了後の描画回数 = tested.RenderCount;

        await tested.InvokeAsync(() => clock.Advance(TimeSpan.FromSeconds(1)));

        tested.RenderCount.Should().Be(終了後の描画回数);
    }

    [Fact]
    public void 記録中の場合は終了時刻の代わりに記録中と表示します()
    {
        var tested = 表示する(null);

        tested.Find(".recording-label").TextContent.Should().Be("記録中");
        tested.FindAll("time").Should().ContainSingle();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void 操作欄にだけ時刻のアクセシブルな名前を付けます(bool 操作欄)
    {
        var tested = context.Render<TimeRecordTimes>(it => it
            .Add(it => it.StartedAt, 開始時刻)
            .Add(it => it.EndedAt, 開始時刻.AddMinutes(90))
            .Add(it => it.Labelled, 操作欄));

        tested.FindAll("time[aria-label]").Should().HaveCount(操作欄 ? 2 : 0);
    }

    IRenderedComponent<TimeRecordTimes> 表示する(DateTimeOffset? 終了時刻) =>
        context.Render<TimeRecordTimes>(it => it
            .Add(it => it.StartedAt, 開始時刻)
            .Add(it => it.EndedAt, 終了時刻));

    public void Dispose() => context.Dispose();
}
