using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.Health;
using Kizamu.Frontend.Blazor.Presentation.Projects;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests;

public sealed class AppTests : IDisposable
{
    static readonly TimeRecord 一つ目の記録 = new(Guid.Parse("019cde5b-4c00-7000-8000-000000000001"), DateTimeOffset.Parse("2026-08-01T09:00:00Z"), null);
    readonly BunitContext context = new();
    readonly ProjectTabsViewModel projectTabs = new();
    readonly TimeRecorderViewModel timeRecorder = new(new StubTimeRecordService(一つ目の記録, 一つ目の記録));

    public AppTests()
    {
        context.Services.AddSingleton(new HealthStatusViewModel(new StubHealthService()));
        context.Services.AddSingleton<ITimeRecorderViewModel>(timeRecorder);
        context.Services.AddSingleton(projectTabs);
        context.Services.AddSingleton(TimeProvider.System);
    }

    [Fact]
    public void 全体の画面にアプリ名と記録操作と接続状態を表示します()
    {
        var tested = context.Render<Client.App>();

        tested.Find("h1").TextContent.Should().Be("Kizamu");
        tested.Find(".recorder-panel button").TextContent.Should().Be("記録を開始");
        tested.Find("footer").TextContent.Should().Contain("サーバー接続: 正常");
    }

    [Fact]
    public void 追加ボタンを押すとプロジェクト追加ポップアップを表示します()
    {
        var tested = context.Render<Client.App>();

        tested.FindAll("button").Single(it => it.TextContent == "追加").Click();

        tested.FindAll("[role=dialog]").Should().ContainSingle();
    }

    [Fact]
    public void プロジェクト追加ポップアップでプロジェクト名を入力できます()
    {
        var tested = 追加ダイアログを表示する();

        tested.Find("[role=dialog] input").Change("プロジェクトA");

        tested.Find("[role=dialog] input").GetAttribute("value").Should().Be("プロジェクトA");
        projectTabs.ProjectName.Should().Be("プロジェクトA");
    }

    [Fact]
    public void プロジェクトの追加をキャンセルするとポップアップを閉じます()
    {
        var tested = 追加ダイアログを表示する("プロジェクトA");

        tested.FindAll("[role=dialog] button").Single(it => it.TextContent == "キャンセル").Click();

        tested.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public void 入力したプロジェクト名を登録するとタブを表示します()
    {
        var tested = 追加ダイアログを表示する("プロジェクトA");

        tested.FindAll("[role=dialog] button").Single(it => it.TextContent == "登録").Click();

        tested.FindAll("[role=tab]").Select(it => it.TextContent).Should().Contain("プロジェクトA");
    }

    [Fact]
    public void 登録済みプロジェクトを選ぶと記録欄にプロジェクト名を表示します()
    {
        var tested = 登録済みプロジェクトのある画面を表示する("プロジェクトA");

        プロジェクトを選ぶ(tested, "プロジェクトA");

        tested.Find(".recorder-panel h2").TextContent.Should().Be("プロジェクトA");
    }

    [Fact]
    public void 登録済みプロジェクトを選ぶと選択中の表示が未指定からそのタブに移ります()
    {
        var tested = 登録済みプロジェクトのある画面を表示する("プロジェクトA");

        プロジェクトを選ぶ(tested, "プロジェクトA");

        tested.FindAll("[role=tab]").Single(it => it.TextContent == "プロジェクトA").GetAttribute("aria-selected").Should().Be("true");
        tested.Find(".project-default").GetAttribute("aria-selected").Should().Be("false");
    }

    [Fact]
    public void プロジェクトを指定せずに記録を開始すると終了操作を表示します()
    {
        var tested = context.Render<Client.App>();

        tested.FindAll("button").Single(it => it.TextContent == "記録を開始").Click();

        tested.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を終了");
    }

    [Fact]
    public void プロジェクトを指定せずに記録を開始すると一覧に記録を表示します()
    {
        var tested = context.Render<Client.App>();

        tested.FindAll("button").Single(it => it.TextContent == "記録を開始").Click();

        tested.FindAll("ul[aria-label='作業記録一覧'] li").Should().ContainSingle();
    }

    [Fact]
    public async Task 記録していないプロジェクトへタブを切り替えると開始操作を表示します()
    {
        var (tested, _) = await プロジェクトAで記録中の画面を表示する();

        プロジェクトを選ぶ(tested, "プロジェクトB");

        tested.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を開始");
    }

    [Fact]
    public async Task タブを切り替えて戻ると記録中の対象には終了操作を表示します()
    {
        var (tested, プロジェクトB) = await プロジェクトAで記録中の画面を表示する();
        projectTabs.SelectProject(プロジェクトB);
        tested.Render();

        プロジェクトを選ぶ(tested, "プロジェクトA");

        tested.FindAll("button").Select(it => it.TextContent).Should().NotContain("記録を開始").And.Contain("記録を終了");
    }

    IRenderedComponent<Client.App> 追加ダイアログを表示する(string プロジェクト名 = "")
    {
        projectTabs.OpenProjectDialog();
        projectTabs.ProjectName = プロジェクト名;
        return context.Render<Client.App>();
    }

    IRenderedComponent<Client.App> 登録済みプロジェクトのある画面を表示する(string プロジェクト名)
    {
        プロジェクトを登録する(プロジェクト名);
        return context.Render<Client.App>();
    }

    async Task<(IRenderedComponent<Client.App> 画面, Project プロジェクトB)> プロジェクトAで記録中の画面を表示する()
    {
        var プロジェクトA = プロジェクトを登録する("プロジェクトA");
        var プロジェクトB = プロジェクトを登録する("プロジェクトB");
        projectTabs.SelectProject(プロジェクトA);
        var tested = context.Render<Client.App>();
        await timeRecorder.StartTimeRecordAsync();
        tested.Render();
        return (tested, プロジェクトB);
    }

    Project プロジェクトを登録する(string name)
    {
        projectTabs.ProjectName = name;
        projectTabs.RegisterProject();
        return projectTabs.Projects[^1];
    }

    static void プロジェクトを選ぶ(IRenderedComponent<Client.App> tested, string name) =>
        tested.FindAll("[role=tab]").Single(it => it.TextContent == name).Click();

    public void Dispose() => context.Dispose();
}
