using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecordTimesTests : IDisposable
{
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    static readonly DateTimeOffset 終了時刻 = 開始時刻.AddMinutes(90);
    readonly BunitContext context = new();
    readonly FakeTimeProvider clock = new(開始時刻);

    IRenderedComponent<TimeRecordTimes> 終了済みの表示 => field ??= 表示する(終了時刻);
    IRenderedComponent<TimeRecordTimes> 記録中の表示 => field ??= 表示する(null);

    public TimeRecordTimesTests() => context.Services.AddSingleton<TimeProvider>(clock);

    [Fact]
    public void 開始時刻と終了時刻を日本時間で表示します()
    {
        var 時刻要素 = 終了済みの表示.FindAll("time");

        時刻要素.Select(it => it.TextContent).Should().Equal("2026/08/01 18:00", "2026/08/01 19:30");
    }

    [Fact]
    public void 開始時刻と終了時刻のdatetime属性はHTMLで有効な日時形式です()
    {
        var 時刻要素 = 終了済みの表示.FindAll("time");

        時刻要素.Should().HaveCount(2);
        時刻要素.Select(it => it.GetAttribute("datetime")).Should().AllSatisfy(it =>
            it.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,3})?)?(?:Z|[+-]\d{2}:\d{2})$"));
    }

    [Fact]
    public void 開始と終了のdatetime属性は元の時刻を示します()
    {
        var 時刻要素 = 終了済みの表示.FindAll("time");

        DateTimeOffset.TryParse(時刻要素[0].GetAttribute("datetime"), out var 開始日時).Should().BeTrue();
        DateTimeOffset.TryParse(時刻要素[1].GetAttribute("datetime"), out var 終了日時).Should().BeTrue();
        開始日時.Should().Be(開始時刻);
        終了日時.Should().Be(終了時刻);
    }

    [Fact]
    public void 小数秒が3桁を超える日時は秒や日付を繰り上げずに切り捨てます()
    {
        var 開始時刻 = DateTimeOffset.Parse("2026-08-01T23:59:59.9999999+09:00");
        var 終了時刻 = DateTimeOffset.Parse("2026-08-02T00:00:59.9999999+09:00");
        var tested = context.Render<TimeRecordTimes>(it => it
            .Add(it => it.StartedAt, 開始時刻)
            .Add(it => it.EndedAt, 終了時刻));

        tested.FindAll("time").Select(it => it.GetAttribute("datetime")).Should().Equal(
            "2026-08-01T23:59:59.999+09:00", "2026-08-02T00:00:59.999+09:00");
    }

    [Fact]
    public async Task 記録中の作業時間が次の整数分になると表示を更新します()
    {
        clock.SetUtcNow(開始時刻.AddMinutes(90).AddSeconds(59));

        await 記録中の表示.InvokeAsync(() => clock.Advance(TimeSpan.FromSeconds(1)));

        記録中の表示.Find(".time-record-duration-value").TextContent.Should().Be("1時間31分");
    }

    [Fact]
    public async Task 終了済みの作業時間は現在時刻が進んでも変わりません()
    {
        await 終了済みの表示.InvokeAsync(() => clock.Advance(TimeSpan.FromHours(1)));

        終了済みの表示.Find(".time-record-duration-value").TextContent.Should().Be("1時間30分");
    }

    [Fact]
    public async Task 記録を終了すると定期的な再描画を停止します()
    {
        var tested = 記録中の表示;
        tested.Render(it => it.Add(it => it.EndedAt, 終了時刻));
        var 終了後の描画回数 = tested.RenderCount;

        await tested.InvokeAsync(() => clock.Advance(TimeSpan.FromSeconds(1)));

        tested.RenderCount.Should().Be(終了後の描画回数);
    }

    [Fact]
    public void 記録中の場合は終了時刻の代わりに記録中と表示します()
    {
        記録中の表示.Find(".recording-label").TextContent.Should().Be("記録中");
        記録中の表示.FindAll("time").Should().ContainSingle();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void 操作欄にだけ時刻のアクセシブルな名前を付けます(bool 操作欄)
    {
        var tested = context.Render<TimeRecordTimes>(it => it
            .Add(it => it.StartedAt, 開始時刻)
            .Add(it => it.EndedAt, 終了時刻)
            .Add(it => it.Labelled, 操作欄));

        tested.FindAll("time[aria-label]").Should().HaveCount(操作欄 ? 2 : 0);
    }

    IRenderedComponent<TimeRecordTimes> 表示する(DateTimeOffset? 終了時刻) =>
        context.Render<TimeRecordTimes>(it => it
            .Add(it => it.StartedAt, 開始時刻)
            .Add(it => it.EndedAt, 終了時刻));

    public void Dispose() => context.Dispose();
}
