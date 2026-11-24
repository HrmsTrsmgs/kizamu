using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.Projects;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.Projects;

public sealed class ProjectRegistrationViewModelTests
{
    readonly ProjectRegistrationViewModel tested = new(_ => { });

    [Fact]
    public void プロジェクトの追加を始めると名前を入力するダイアログが開きます()
    {
        tested.Open();

        tested.IsOpen.Should().BeTrue();
    }

    [Fact]
    public void プロジェクトの追加をキャンセルすると追加ダイアログが閉じます()
    {
        名前を入力した追加ダイアログを開く("プロジェクトA");

        tested.Cancel();

        tested.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void プロジェクトの追加をキャンセルして開き直すと入力中の名前が残ります()
    {
        名前を入力した追加ダイアログを開く("プロジェクトA");

        tested.Cancel();
        tested.Open();

        tested.ProjectName.Should().Be("プロジェクトA");
    }

    [Fact]
    public void 追加ダイアログでプロジェクトを登録するとダイアログが閉じます()
    {
        名前を入力した追加ダイアログを開く("プロジェクトA");

        tested.Register();

        tested.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void プロジェクトの登録後に追加ダイアログを開き直すと名前の入力欄は空です()
    {
        プロジェクトを登録しておく("プロジェクトA");

        tested.Open();

        tested.ProjectName.Should().BeEmpty();
    }

    void プロジェクトを登録しておく(string name)
    {
        名前を入力した追加ダイアログを開く(name);
        tested.Register();
    }

    void 名前を入力した追加ダイアログを開く(string name)
    {
        tested.Open();
        tested.ProjectName = name;
    }
}
