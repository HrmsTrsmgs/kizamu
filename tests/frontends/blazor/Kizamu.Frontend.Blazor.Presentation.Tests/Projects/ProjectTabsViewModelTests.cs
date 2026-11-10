using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.Projects;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.Projects;

public sealed class ProjectTabsViewModelTests
{
    [Fact]
    public void 追加をキャンセルすると登録せずダイアログを閉じる()
    {
        var tested = new ProjectTabsViewModel();
        tested.OpenProjectDialog();
        tested.ProjectName = "プロジェクトA";

        tested.CancelProjectDialog();

        tested.ShowProjectDialog.Should().BeFalse();
        tested.Projects.Should().BeEmpty();
    }

    [Fact]
    public void 追加を開いて名前を登録すると一覧に追加しダイアログを閉じる()
    {
        var tested = new ProjectTabsViewModel();

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
        var tested = new ProjectTabsViewModel();
        tested.OpenProjectDialog();
        tested.ProjectName = "プロジェクトA";
        tested.RegisterProject();

        tested.OpenProjectDialog();

        tested.ProjectName.Should().BeEmpty();
    }
}
