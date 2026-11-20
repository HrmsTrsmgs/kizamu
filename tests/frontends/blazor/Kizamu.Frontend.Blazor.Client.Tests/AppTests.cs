using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Client.Health;
using Kizamu.Frontend.Blazor.Presentation.Health;
using Kizamu.Frontend.Blazor.Presentation.Projects;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests;

public sealed class AppTests : IDisposable
{
    static readonly TimeRecord 未終了の記録 = new(Guid.Parse("019cde5b-4c00-7000-8000-000000000001"), DateTimeOffset.Parse("2026-08-01T09:00:00Z"), null);
    readonly BunitContext context = new();
    readonly ProjectTabsViewModel projectTabs = new();
    readonly StubTimeRecordService 記録サービス = new(未終了の記録, 未終了の記録);

    IRenderedComponent<Client.App> 未ログインの画面 => field ??= context.Render<Client.App>();
    IRenderedComponent<Client.App> ログイン操作用の画面 => field ??= ログイン可能な画面を表示する();
    IRenderedComponent<Client.App> ログイン済みの画面 =>
        field ??= context.Render<Client.App>(parameters => parameters.Add(it => it.IsAuthenticated, true));

    public AppTests()
    {
        context.Services.AddSingleton(new HealthStatusViewModel(new StubHealthService()));
        context.Services.AddSingleton<ITimeRecorderViewModel>(new TimeRecorderViewModel(記録サービス));
        context.Services.AddSingleton(projectTabs);
        context.Services.AddSingleton(TimeProvider.System);
    }

    [Fact]
    public void 未ログイン時はGoogleのログインボタンを表示します()
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

        ログイン操作用の画面.FindAll(".recorder-panel").Should().ContainSingle();
    }

    [Fact]
    public void Googleログインに成功するとログインボタンが消えます()
    {
        ログインする(ログイン操作用の画面);

        ログイン操作用の画面.FindAll("button").Select(it => it.TextContent).Should().NotContain("Googleでログイン");
    }

    [Fact]
    public void Googleログインで取得したIDトークンを記録の通信サービスに渡します()
    {
        string? 記録通信に渡されたトークン = null;
        var tested = ログイン可能な画面を表示する(token =>
        {
            記録通信に渡されたトークン = token;
            return 記録サービス;
        });

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
    public void ログアウトするとGoogleのログインボタンを表示します()
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
    public void 再ログインすると前の記録が一覧から消えて記録欄のボタンが開始に戻ります()
    {
        var ログインごとの記録サービス = new Queue<StubTimeRecordService>(
        [
            new(未終了の記録, 未終了の記録) { Records = [未終了の記録] },
            new(未終了の記録, 未終了の記録),
        ]);
        var tested = ログイン可能な画面を表示する(_ => ログインごとの記録サービス.Dequeue());
        ログインする(tested);
        tested.FindAll("ul[aria-label='作業記録一覧'] li").Should().ContainSingle();
        tested.FindAll(".recorder-panel .record-button").Should().ContainSingle().Which.TextContent.Should().Be("記録を終了");
        ログアウトする(tested);

        ログインする(tested);

        tested.FindAll("ul[aria-label='作業記録一覧'] li").Should().BeEmpty();
        tested.FindAll(".recorder-panel .record-button").Should().ContainSingle().Which.TextContent.Should().Be("記録を開始");
    }

    [Fact]
    public void 初期認証済みの画面でログアウトすると作業記録一覧が消えてGoogleのログインボタンが表示されます()
    {
        var tested = ログイン済みの画面;
        tested.FindAll("ul[aria-label='作業記録一覧']").Should().ContainSingle();

        ログアウトする(tested);

        tested.FindAll("ul[aria-label='作業記録一覧']").Should().BeEmpty();
        tested.FindAll("button").Select(it => it.TextContent).Should().Contain("Googleでログイン");
    }

    [Fact]
    public void 画面の見出しにアプリ名を表示します()
    {
        ログイン済みの画面.Find("h1").TextContent.Should().Be("Kizamu");
    }

    [Fact]
    public void 画面のフッターにサーバー接続状態のコンポーネントを配置します()
    {
        context.ComponentFactories.AddStub<HealthStatus>("<p data-testid='health-status'></p>");

        ログイン済みの画面.FindAll("footer [data-testid='health-status']").Should().ContainSingle();
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
    public void 入力したプロジェクト名で登録するとその名前のタブが表示されます()
    {
        追加ダイアログを開いておく("プロジェクトA");

        ログイン済みの画面.FindAll("[role=dialog] button").Single(it => it.TextContent == "登録").Click();

        ログイン済みの画面.FindAll("[role=tab]").Select(it => it.TextContent).Should().Contain("プロジェクトA");
    }

    [Fact]
    public void 登録済みプロジェクトと未指定を切り替えると選択表示と記録欄が切り替わります()
    {
        プロジェクトを登録する("プロジェクトA");
        記録サービス.Records = [未終了の記録];
        var tested = ログイン済みの画面;
        tested.Find("[role=tablist] [aria-selected=true]").TextContent.Should().Be("未指定");
        tested.Find(".recorder-panel .record-button").TextContent.Should().Be("記録を終了");

        プロジェクトを選ぶ(tested, "プロジェクトA");

        tested.FindAll("[role=tablist] [aria-selected=true]").Should().ContainSingle().Which.TextContent.Should().Be("プロジェクトA");
        tested.Find(".project-default").GetAttribute("aria-selected").Should().Be("false");
        tested.Find(".recorder-panel h2").TextContent.Should().Be("プロジェクトA");
        tested.Find(".recorder-panel .record-button").TextContent.Should().Be("記録を開始");

        プロジェクトを選ぶ(tested, "未指定");

        tested.FindAll("[role=tablist] [aria-selected=true]").Should().ContainSingle().Which.TextContent.Should().Be("未指定");
        tested.FindAll("[role=tab]").Single(it => it.TextContent == "プロジェクトA").GetAttribute("aria-selected").Should().Be("false");
        tested.Find(".recorder-panel h2").TextContent.Should().Be("プロジェクト未指定");
        tested.Find(".recorder-panel .record-button").TextContent.Should().Be("記録を終了");
    }

    IRenderedComponent<Client.App> ログイン可能な画面を表示する(Func<string, ITimeRecordService>? 記録サービスを作る = null) =>
        context.Render<Client.App>(parameters => parameters
            .Add(it => it.SignInWithGoogle, () => Task.FromResult("google-id-token"))
            .Add(it => it.CreateTimeRecords, 記録サービスを作る ?? (_ => new StubTimeRecordService(未終了の記録, 未終了の記録))));

    static void ログインする(IRenderedComponent<Client.App> tested) =>
        tested.FindAll("button").Single(it => it.TextContent == "Googleでログイン").Click();

    static void ログアウトする(IRenderedComponent<Client.App> tested) =>
        tested.FindAll("button").Single(it => it.TextContent == "ログアウト").Click();

    static int 認証ボタンの位置(IRenderedComponent<Client.App> tested, string ボタン名) =>
        tested.Find("main").Children.ToList().FindIndex(it =>
            it.Children.Any(child => child.LocalName == "button" && child.TextContent == ボタン名));

    void 追加ダイアログを開いておく(string プロジェクト名 = "")
    {
        projectTabs.OpenProjectDialog();
        projectTabs.ProjectName = プロジェクト名;
    }

    void プロジェクトを登録する(string name)
    {
        projectTabs.ProjectName = name;
        projectTabs.RegisterProject();
    }

    static void プロジェクトを選ぶ(IRenderedComponent<Client.App> tested, string name) =>
        tested.FindAll("[role=tab]").Single(it => it.TextContent == name).Click();

    public void Dispose() => context.Dispose();
}
