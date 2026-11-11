using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.Projects;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.Projects;

public sealed class ProjectTabsViewModelTests
{
    readonly ProjectTabsViewModel tested;

    public ProjectTabsViewModelTests() => tested = new();

    [Fact]
    public void 追加をキャンセルすると登録せずダイアログを閉じる()
    {
        tested.OpenProjectDialog();
        tested.ProjectName = "プロジェクトA";

        tested.CancelProjectDialog();

        tested.ShowProjectDialog.Should().BeFalse();
        tested.Projects.Should().BeEmpty();
    }

    [Fact]
    public void 追加を開いて名前を登録すると一覧に追加しダイアログを閉じる()
    {
        tested.OpenProjectDialog();
        tested.ShowProjectDialog.Should().BeTrue();
        tested.ProjectName = "プロジェクトA";
        tested.RegisterProject();

        tested.Projects.Should().ContainSingle().Which.Name.Should().Be("プロジェクトA");
        tested.ShowProjectDialog.Should().BeFalse();
    }
    [Fact]
    public void 登録後に追加を開くとプロジェクト名は空になる()
    {
        プロジェクトを登録しておく("プロジェクトA");

        tested.OpenProjectDialog();

        tested.ProjectName.Should().BeEmpty();
    }

    void プロジェクトを登録しておく(string name)
    {
        tested.OpenProjectDialog();
        tested.ProjectName = name;
        tested.RegisterProject();
    }
}
