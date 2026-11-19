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

    IRenderedComponent<Client.App> 未ログインの画面 => field ??= context.Render<Client.App>();
    IRenderedComponent<Client.App> ログイン操作用の画面 => field ??= ログイン可能な画面を表示する();
    IRenderedComponent<Client.App> ログイン済みの画面 =>
        field ??= context.Render<Client.App>(parameters => parameters.Add(it => it.IsAuthenticated, true));

    public AppTests()
    {
        context.Services.AddSingleton(new HealthStatusViewModel(new StubHealthService()));
        context.Services.AddSingleton<ITimeRecorderViewModel>(timeRecorder);
        context.Services.AddSingleton(projectTabs);
        context.Services.AddSingleton(TimeProvider.System);
    }

    [Fact]
    public void 未ログイン時はGoogleログインを表示します()
    {
        未ログインの画面.FindAll("button").Select(it => it.TextContent).Should().Contain("Googleでログイン");
    }

    [Fact]
    public void 未ログイン時は記録画面を表示しません()
    {
        未ログインの画面.FindAll(".recorder-panel").Should().BeEmpty();
    }

    [Fact]
    public void Googleログインに成功すると記録画面を表示します()
    {
        ログインする(ログイン操作用の画面);

        ログイン操作用の画面.FindAll(".recorder-panel button").Select(it => it.TextContent).Should().Contain("記録を開始");
    }

    [Fact]
    public void Googleログインに成功するとログインボタンが消えます()
    {
        ログインする(ログイン操作用の画面);

        ログイン操作用の画面.FindAll("button").Select(it => it.TextContent).Should().NotContain("Googleでログイン");
    }

    [Fact]
    public void Googleログインで取得したトークンを記録通信へ渡します()
    {
        string? 記録通信に渡されたトークン = null;
        var tested = ログイン可能な画面を表示する(token => 記録通信に渡されたトークン = token);

        ログインする(tested);

        記録通信に渡されたトークン.Should().Be("google-id-token");
    }

    [Fact]
    public void ログアウトボタンはログインボタンと同じ場所に表示します()
    {
        var ログインの場所 = 認証ボタンの位置(ログイン操作用の画面, "Googleでログイン");
        ログインの場所.Should().BeGreaterThanOrEqualTo(0);

        ログインする(ログイン操作用の画面);

        認証ボタンの位置(ログイン操作用の画面, "ログアウト").Should().Be(ログインの場所);
    }

    [Fact]
    public void ログアウトするとGoogleログインを表示します()
    {
        ログインする(ログイン操作用の画面);

        ログアウトする(ログイン操作用の画面);

        ログイン操作用の画面.FindAll("button").Select(it => it.TextContent).Should().Contain("Googleでログイン");
    }

    [Fact]
    public void ログアウトすると記録画面を閉じます()
    {
        ログインする(ログイン操作用の画面);

        ログアウトする(ログイン操作用の画面);

        ログイン操作用の画面.FindAll(".recorder-panel").Should().BeEmpty();
    }

    [Fact]
    public void 再ログインすると開始操作に戻ります()
    {
        記録中にログアウトしておく();

        ログインする(ログイン操作用の画面);

        ログイン操作用の画面.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を開始").And.NotContain("記録を終了");
    }

    [Fact]
    public void 再ログインすると前のログイン時の記録を一覧に表示しません()
    {
        記録中にログアウトしておく();

        ログインする(ログイン操作用の画面);

        ログイン操作用の画面.FindAll("ul[aria-label='作業記録一覧'] li").Should().BeEmpty();
    }

    [Fact]
    public void 画面の見出しにアプリ名を表示します()
    {
        ログイン済みの画面.Find("h1").TextContent.Should().Be("Kizamu");
    }

    [Fact]
    public void ログイン済みの画面に記録操作を表示します()
    {
        ログイン済みの画面.Find(".recorder-panel button").TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 画面のフッターに接続状態を表示します()
    {
        ログイン済みの画面.Find("footer").TextContent.Should().Contain("サーバー接続: 正常");
    }

    [Fact]
    public void 追加ボタンを押すとプロジェクト追加ポップアップを表示します()
    {
        ログイン済みの画面.FindAll("button").Single(it => it.TextContent == "追加").Click();

        ログイン済みの画面.FindAll("[role=dialog]").Should().ContainSingle();
    }

    [Fact]
    public void プロジェクト追加ポップアップに入力した名前を表示します()
    {
        追加ダイアログを開いておく();

        ログイン済みの画面.Find("[role=dialog] input").Change("プロジェクトA");

        ログイン済みの画面.Find("[role=dialog] input").GetAttribute("value").Should().Be("プロジェクトA");
    }

    [Fact]
    public void プロジェクト追加ポップアップの入力を登録する名前に反映します()
    {
        追加ダイアログを開いておく();

        ログイン済みの画面.Find("[role=dialog] input").Change("プロジェクトA");

        projectTabs.ProjectName.Should().Be("プロジェクトA");
    }

    [Fact]
    public void プロジェクトの追加をキャンセルするとポップアップを閉じます()
    {
        追加ダイアログを開いておく("プロジェクトA");

        ログイン済みの画面.FindAll("[role=dialog] button").Single(it => it.TextContent == "キャンセル").Click();

        ログイン済みの画面.FindAll("[role=dialog]").Should().BeEmpty();
    }

    [Fact]
    public void 入力したプロジェクト名を登録するとタブを表示します()
    {
        追加ダイアログを開いておく("プロジェクトA");

        ログイン済みの画面.FindAll("[role=dialog] button").Single(it => it.TextContent == "登録").Click();

        ログイン済みの画面.FindAll("[role=tab]").Select(it => it.TextContent).Should().Contain("プロジェクトA");
    }

    [Fact]
    public void 登録済みプロジェクトを選ぶと記録欄にプロジェクト名を表示します()
    {
        プロジェクトを登録する("プロジェクトA");

        プロジェクトを選ぶ(ログイン済みの画面, "プロジェクトA");

        ログイン済みの画面.Find(".recorder-panel h2").TextContent.Should().Be("プロジェクトA");
    }

    [Fact]
    public void 登録済みプロジェクトを選ぶと選択中の表示が未指定からそのタブに移ります()
    {
        プロジェクトを登録する("プロジェクトA");

        プロジェクトを選ぶ(ログイン済みの画面, "プロジェクトA");

        ログイン済みの画面.FindAll("[role=tab]").Single(it => it.TextContent == "プロジェクトA").GetAttribute("aria-selected").Should().Be("true");
        ログイン済みの画面.Find(".project-default").GetAttribute("aria-selected").Should().Be("false");
    }

    [Fact]
    public void プロジェクトを指定せずに記録を開始すると終了操作を表示します()
    {
        ログイン済みの画面.FindAll("button").Single(it => it.TextContent == "記録を開始").Click();

        ログイン済みの画面.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を終了");
    }

    [Fact]
    public void プロジェクトを指定せずに記録を開始すると一覧に記録を表示します()
    {
        ログイン済みの画面.FindAll("button").Single(it => it.TextContent == "記録を開始").Click();

        ログイン済みの画面.FindAll("ul[aria-label='作業記録一覧'] li").Should().ContainSingle();
    }

    [Fact]
    public async Task 記録していないプロジェクトへタブを切り替えると開始操作を表示します()
    {
        await プロジェクトAで記録を開始しておく();

        プロジェクトを選ぶ(ログイン済みの画面, "プロジェクトB");

        ログイン済みの画面.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を開始");
    }

    [Fact]
    public async Task タブを切り替えて戻ると記録中の対象には終了操作を表示します()
    {
        var プロジェクトB = await プロジェクトAで記録を開始しておく();
        projectTabs.SelectProject(プロジェクトB);
        ログイン済みの画面.Render();

        プロジェクトを選ぶ(ログイン済みの画面, "プロジェクトA");

        ログイン済みの画面.FindAll("button").Select(it => it.TextContent).Should().NotContain("記録を開始").And.Contain("記録を終了");
    }

    IRenderedComponent<Client.App> ログイン可能な画面を表示する(Action<string>? トークンを受け取る = null) =>
        context.Render<Client.App>(parameters => parameters
            .Add(it => it.SignInWithGoogle, () => Task.FromResult("google-id-token"))
            .Add(it => it.CreateTimeRecords, token =>
            {
                トークンを受け取る?.Invoke(token);
                return new StubTimeRecordService(一つ目の記録, 一つ目の記録);
            }));

    static void ログインする(IRenderedComponent<Client.App> tested) =>
        tested.FindAll("button").Single(it => it.TextContent == "Googleでログイン").Click();

    static void ログアウトする(IRenderedComponent<Client.App> tested) =>
        tested.FindAll("button").Single(it => it.TextContent == "ログアウト").Click();

    void 記録中にログアウトしておく()
    {
        var tested = ログイン操作用の画面;
        ログインする(tested);
        tested.FindAll("button").Single(it => it.TextContent == "記録を開始").Click();
        tested.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を終了");
        ログアウトする(tested);
    }

    static int 認証ボタンの位置(IRenderedComponent<Client.App> tested, string ボタン名) =>
        tested.Find("main").Children.ToList().FindIndex(it =>
            it.Children.Any(child => child.LocalName == "button" && child.TextContent == ボタン名));

    void 追加ダイアログを開いておく(string プロジェクト名 = "")
    {
        projectTabs.OpenProjectDialog();
        projectTabs.ProjectName = プロジェクト名;
    }

    async Task<Project> プロジェクトAで記録を開始しておく()
    {
        var プロジェクトA = プロジェクトを登録する("プロジェクトA");
        var プロジェクトB = プロジェクトを登録する("プロジェクトB");
        projectTabs.SelectProject(プロジェクトA);
        var tested = ログイン済みの画面;
        await timeRecorder.StartTimeRecordAsync();
        tested.Render();
        return プロジェクトB;
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
