using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Client.TimeRecords;
using Microsoft.AspNetCore.Components;

namespace Kizamu.Frontend.Blazor.Client.Tests.TimeRecords;

public sealed class TimeRecorderPanelTests
{
    [Fact]
    public void 二つの操作欄はそれぞれのプロジェクトの見出しを参照します()
    {
        using var context = new BunitContext();
        var tested = 二つの操作欄を表示する(context, "プロジェクトA", "プロジェクトB");

        var 見出し = tested.FindAll("h2");
        var 見出しのID =
            from heading in 見出し
            select heading.Id;
        var 見出しへの参照 =
            from panel in tested.FindAll(".recorder-panel")
            select panel.GetAttribute("aria-labelledby");

        var 見出しの表示 =
            from heading in 見出し
            select heading.TextContent;

        見出しの表示.Should().Equal("プロジェクトA", "プロジェクトB");
        見出しのID.Should().OnlyHaveUniqueItems();
        見出しへの参照.Should().Equal(見出しのID);
    }

    static IRenderedComponent<IComponent> 二つの操作欄を表示する(BunitContext context, string firstProject, string secondProject) =>
        context.Render(builder =>
        {
            builder.OpenComponent<TimeRecorderPanel>(0);
            builder.AddComponentParameter(1, nameof(TimeRecorderPanel.ProjectName), firstProject);
            builder.CloseComponent();

            builder.OpenComponent<TimeRecorderPanel>(2);
            builder.AddComponentParameter(3, nameof(TimeRecorderPanel.ProjectName), secondProject);
            builder.CloseComponent();
        });
}
