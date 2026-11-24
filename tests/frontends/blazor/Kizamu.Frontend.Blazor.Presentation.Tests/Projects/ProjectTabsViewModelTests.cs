using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.Projects;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.Projects;

public sealed class ProjectTabsViewModelTests
{
    readonly ProjectTabsViewModel tested = new();

    [Fact]
    public void プロジェクトを選ぶとそのタブが選択され未指定タブを選ぶと選択対象は未指定に戻ります()
    {
        var プロジェクト = new Project("プロジェクトA");

        tested.SelectProject(プロジェクト);

        tested.SelectedProject.Should().BeSameAs(プロジェクト);

        tested.SelectProject(null);

        tested.SelectedProject.Should().BeNull();
    }

    [Fact]
    public void プロジェクトの追加をキャンセルすると入力した名前のプロジェクトは登録されません()
    {
        名前を入力した追加ダイアログを開く("プロジェクトA");

        tested.Registration.Cancel();

        tested.Projects.Should().BeEmpty();
    }

    [Fact]
    public void プロジェクトを登録すると入力した名前のプロジェクトが一覧に加わります()
    {
        tested.Registration.ProjectName = "プロジェクトA";

        tested.Registration.Register();

        tested.Projects.Should().ContainSingle().Which.Name.Should().Be("プロジェクトA");
    }

    void 名前を入力した追加ダイアログを開く(string name)
    {
        tested.Registration.Open();
        tested.Registration.ProjectName = name;
    }
}
