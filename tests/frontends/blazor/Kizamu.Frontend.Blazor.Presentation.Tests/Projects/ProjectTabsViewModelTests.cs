using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.Projects;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.Projects;

public sealed class ProjectTabsViewModelTests
{
    [Fact]
    public void プロジェクトを選択すると選択したプロジェクトを保持する()
    {
        var tested = new ProjectTabsViewModel();
        var プロジェクト = new Project("プロジェクトA");

        tested.SelectProject(プロジェクト);

        tested.SelectedProject.Should().BeSameAs(プロジェクト);
    }

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
    public void 追加を開くとダイアログを表示する()
    {
        var tested = new ProjectTabsViewModel();

        tested.OpenProjectDialog();

        tested.ShowProjectDialog.Should().BeTrue();
    }

    [Fact]
    public void 入力した名前を登録すると一覧に追加する()
    {
        var tested = new ProjectTabsViewModel { ProjectName = "プロジェクトA" };

        tested.RegisterProject();

        tested.Projects.Should().ContainSingle().Which.Name.Should().Be("プロジェクトA");
    }

    [Fact]
    public void 追加画面から登録するとダイアログを閉じる()
    {
        var tested = new ProjectTabsViewModel();
        tested.OpenProjectDialog();
        tested.ProjectName = "プロジェクトA";

        tested.RegisterProject();

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
