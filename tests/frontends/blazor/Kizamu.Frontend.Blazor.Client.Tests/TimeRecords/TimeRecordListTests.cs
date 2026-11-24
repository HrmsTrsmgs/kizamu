using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecordListTests : IDisposable
{
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    readonly BunitContext context = new();

    public TimeRecordListTests()
    {
        context.Services.AddSingleton<TimeProvider>(new FakeTimeProvider(開始時刻.AddHours(2)));
    }

    [Fact]
    public void 二つの記録一覧はそれぞれの見出しを参照します()
    {
        var tested = 二つの作業記録一覧を表示する();

        var 見出しのID =
            from heading in tested.FindAll("h2")
            select heading.Id;
        var 見出しへの参照 =
            from list in tested.FindAll(".records-panel")
            select list.GetAttribute("aria-labelledby");

        見出しのID.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        見出しへの参照.Should().Equal(見出しのID);
    }

    [Fact]
    public void 記録一覧には渡された順に各記録の開始時刻を表示します()
    {
        var tested = context.Render<TimeRecordList>(parameters => parameters.Add(
            it => it.Records,
            [TimeRecord.Start(開始時刻.AddHours(1)), TimeRecord.Start(開始時刻)]));

        var 開始時刻の表示 =
            from item in tested.FindAll("ul[aria-label='作業記録一覧'] li")
            select item.QuerySelector("time")?.TextContent;
        開始時刻の表示.Should().Equal("2026/08/01 19:00", "2026/08/01 18:00");
    }

    IRenderedComponent<IComponent> 二つの作業記録一覧を表示する() =>
        context.Render(builder =>
        {
            builder.OpenComponent<TimeRecordList>(0);
            builder.AddComponentParameter(1, nameof(TimeRecordList.Records), Array.Empty<TimeRecord>());
            builder.CloseComponent();

            builder.OpenComponent<TimeRecordList>(2);
            builder.AddComponentParameter(3, nameof(TimeRecordList.Records), Array.Empty<TimeRecord>());
            builder.CloseComponent();
        });

    public void Dispose() => context.Dispose();
}
