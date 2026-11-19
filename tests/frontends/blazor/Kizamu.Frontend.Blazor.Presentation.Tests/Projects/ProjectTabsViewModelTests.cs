using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.Projects;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.Projects;

public sealed class ProjectTabsViewModelTests
{
    readonly ProjectTabsViewModel tested = new();

    [Fact]
    public void プロジェクトを選ぶとそのプロジェクトがタブの選択対象になります()
    {
        var プロジェクト = new Project("プロジェクトA");

        tested.SelectProject(プロジェクト);

        tested.SelectedProject.Should().BeSameAs(プロジェクト);
    }

    [Fact]
    public void プロジェクトの追加をキャンセルすると追加ダイアログが閉じます()
    {
        名前を入力した追加ダイアログを開く("プロジェクトA");

        tested.CancelProjectDialog();

        tested.ShowProjectDialog.Should().BeFalse();
    }

    [Fact]
    public void プロジェクトの追加をキャンセルすると入力した名前のプロジェクトは登録されません()
    {
        名前を入力した追加ダイアログを開く("プロジェクトA");

        tested.CancelProjectDialog();

        tested.Projects.Should().BeEmpty();
    }

    [Fact]
    public void プロジェクトの追加を始めると名前を入力するダイアログが開きます()
    {
        tested.OpenProjectDialog();

        tested.ShowProjectDialog.Should().BeTrue();
    }

    [Fact]
    public void プロジェクトを登録すると入力した名前のプロジェクトが一覧に加わります()
    {
        tested.ProjectName = "プロジェクトA";

        tested.RegisterProject();

        tested.Projects.Should().ContainSingle().Which.Name.Should().Be("プロジェクトA");
    }

    [Fact]
    public void 追加ダイアログでプロジェクトを登録するとダイアログが閉じます()
    {
        名前を入力した追加ダイアログを開く("プロジェクトA");

        tested.RegisterProject();

        tested.ShowProjectDialog.Should().BeFalse();
    }

    [Fact]
    public void プロジェクトの登録後に追加ダイアログを開き直すと名前の入力欄は空です()
    {
        プロジェクトを登録しておく("プロジェクトA");

        tested.OpenProjectDialog();

        tested.ProjectName.Should().BeEmpty();
    }

    void プロジェクトを登録しておく(string name)
    {
        名前を入力した追加ダイアログを開く(name);
        tested.RegisterProject();
    }

    void 名前を入力した追加ダイアログを開く(string name)
    {
        tested.OpenProjectDialog();
        tested.ProjectName = name;
    }
}
