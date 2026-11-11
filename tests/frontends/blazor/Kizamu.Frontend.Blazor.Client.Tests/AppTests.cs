using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Infrastructure.Health;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;
using Kizamu.Frontend.Blazor.Presentation.Projects;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests;

public sealed class AppTests : IDisposable
{
    static readonly TimeRecord 一つ目の記録 = new(Guid.Parse("019cde5b-4c00-7000-8000-000000000001"), DateTimeOffset.Parse("2026-08-01T09:00:00Z"), null);
    readonly BunitContext context = new();
    readonly HttpClient client;
    readonly ProjectTabsViewModel projectTabs = new();

    public AppTests()
    {
        var handler = new TestHttpMessageHandler()
        {
            Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = it.RequestUri?.AbsolutePath == "/api/health"
                    ? JsonContent.Create(new { status = "ok" })
                    : JsonContent.Create(Array.Empty<TimeRecordResponse>())
            })
        };
        client = new(handler) { BaseAddress = new("http://localhost/") };
        context.Services.AddSingleton(new HealthApi(client));
        context.Services.AddSingleton<ITimeRecordService>(new TimeRecordApi(client));
        context.Services.AddTransient<TimeRecorderViewModel>();
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
    public void プロジェクトの追加をキャンセルすると登録せずポップアップを閉じます()
    {
        var tested = context.Render<Client.App>();

        tested.FindAll("button").Single(it => it.TextContent == "追加").Click();
        var popup = tested.Find("[role=dialog]");
        var nameInput = popup.QuerySelector("input");
        nameInput.Should().NotBeNull();
        nameInput.Change("プロジェクトA");
        popup.QuerySelectorAll("button").Single(it => it.TextContent == "キャンセル").Click();

        tested.FindAll("[role=dialog]").Should().BeEmpty();
        tested.FindAll("[role=tab]").Select(it => it.TextContent).Should().NotContain("プロジェクトA");
    }

    [Fact]
    public void 追加ボタンで開いたポップアップにプロジェクト名を入力して登録するとタブを表示します()
    {
        var tested = context.Render<Client.App>();

        tested.FindAll("button").Single(it => it.TextContent == "追加").Click();
        var popup = tested.Find("[role=dialog]");
        var nameInput = popup.QuerySelector("input");
        nameInput.Should().NotBeNull();
        nameInput.Change("プロジェクトA");
        popup.QuerySelectorAll("button").Single(it => it.TextContent == "登録").Click();

        tested.FindAll("[role=tab]").Select(it => it.TextContent).Should().Contain("プロジェクトA");
    }

    [Fact]
    public void 登録済みプロジェクトを選ぶと記録欄にプロジェクト名を表示します()
    {
        プロジェクトを登録する(projectTabs, "プロジェクトA");
        var tested = context.Render<Client.App>();

        プロジェクトを選ぶ(tested, "プロジェクトA");

        tested.Find(".recorder-panel h2").TextContent.Should().Be("プロジェクトA");
    }

    [Fact]
    public void 登録済みプロジェクトを選ぶとそのタブが選択中になり未指定の選択が解除されます()
    {
        プロジェクトを登録する(projectTabs, "プロジェクトA");
        var tested = context.Render<Client.App>();

        プロジェクトを選ぶ(tested, "プロジェクトA");

        tested.FindAll("[role=tab]").Single(it => it.TextContent == "プロジェクトA").GetAttribute("aria-selected").Should().Be("true");
        tested.Find(".project-default").GetAttribute("aria-selected").Should().Be("false");
    }

    [Fact]
    public void プロジェクトを指定せずに記録を開始できます()
    {
        context.Services.AddSingleton<ITimeRecordService>(new StubTimeRecordService(一つ目の記録, 一つ目の記録));
        var tested = context.Render<Client.App>();

        tested.FindAll("button").Single(it => it.TextContent == "記録を開始").Click();

        tested.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を終了");
        tested.FindAll("ul[aria-label='作業記録一覧'] li").Should().ContainSingle();
    }

    [Fact]
    public void タブを切り替えて戻ると記録中の対象には終了操作を表示します()
    {
        context.Services.AddSingleton<ITimeRecordService>(new StubTimeRecordService(一つ目の記録, 一つ目の記録));
        プロジェクトを登録する(projectTabs, "プロジェクトA");
        プロジェクトを登録する(projectTabs, "プロジェクトB");
        var tested = context.Render<Client.App>();
        プロジェクトを選ぶ(tested, "プロジェクトA");
        tested.FindAll("button").Single(it => it.TextContent == "記録を開始").Click();

        プロジェクトを選ぶ(tested, "プロジェクトB");
        tested.FindAll("button").Select(it => it.TextContent).Should().Contain("記録を開始");
        プロジェクトを選ぶ(tested, "プロジェクトA");
        tested.FindAll("button").Select(it => it.TextContent).Should().NotContain("記録を開始").And.Contain("記録を終了");
    }

    static void プロジェクトを登録する(ProjectTabsViewModel viewModel, string name)
    {
        viewModel.ProjectName = name;
        viewModel.RegisterProject();
    }

    static void プロジェクトを選ぶ(IRenderedComponent<Client.App> tested, string name) =>
        tested.FindAll("[role=tab]").Single(it => it.TextContent == name).Click();

    public void Dispose()
    {
        context.Dispose();
        client.Dispose();
    }
}
