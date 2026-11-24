using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Client.Projects;
using Microsoft.AspNetCore.Components;

namespace Kizamu.Frontend.Blazor.Client.Tests.Projects;

public sealed class ProjectRegistrationDialogTests : IDisposable
{
    readonly BunitContext context = new();
    readonly IRenderedComponent<IComponent> tested;

    public ProjectRegistrationDialogTests()
    {
        tested = context.Render(builder =>
        {
            builder.OpenComponent<ProjectRegistrationDialog>(0);
            builder.AddComponentParameter(1, nameof(ProjectRegistrationDialog.ProjectName), "");
            builder.CloseComponent();

            builder.OpenComponent<ProjectRegistrationDialog>(2);
            builder.AddComponentParameter(3, nameof(ProjectRegistrationDialog.ProjectName), "");
            builder.CloseComponent();
        });
    }

    [Fact]
    public void 二つの登録ダイアログはそれぞれの見出しを参照します()
    {
        var 見出しのID =
            from heading in tested.FindAll("h2")
            select heading.Id;
        var 見出しへの参照 =
            from dialog in tested.FindAll("[role=dialog]")
            select dialog.GetAttribute("aria-labelledby");

        見出しのID.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        見出しへの参照.Should().Equal(見出しのID);
    }

    [Fact]
    public void 二つの登録ダイアログのラベルはそれぞれの名前入力欄を参照します()
    {
        var 名前入力欄のID =
            from input in tested.FindAll("input")
            select input.Id;
        var ラベルの参照先 =
            from label in tested.FindAll("label")
            select label.GetAttribute("for");

        名前入力欄のID.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        ラベルの参照先.Should().Equal(名前入力欄のID);
    }

    public void Dispose() => context.Dispose();
}
