using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Infrastructure.Health;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests;

public sealed class AppTests : IDisposable
{
    readonly BunitContext context = new();
    readonly HttpClient client;

    public AppTests()
    {
        client = new(new TestHttpMessageHandler
        {
            Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = it.RequestUri?.AbsolutePath == "/api/health"
                    ? JsonContent.Create(new { status = "ok" })
                    : JsonContent.Create(Array.Empty<TimeRecordResponse>())
            })
        })
        { BaseAddress = new("http://localhost/") };
        context.Services.AddSingleton(new HealthApi(client));
        context.Services.AddSingleton<ITimeRecordService>(new TimeRecordApi(client));
        context.Services.AddTransient<TimeRecorderViewModel>();
        context.Services.AddSingleton(TimeProvider.System);
    }

    [Fact]
    public void 全体の画面にアプリ名と記録操作と接続状態を表示します()
    {
        var tested = context.Render<Client.App>();

        tested.Find("h1").TextContent.Should().Be("Kizamu");
        tested.Find("button").TextContent.Should().Be("記録を開始");
        tested.Find("footer").TextContent.Should().Contain("サーバー接続: 正常");
    }

    [Fact(Skip = "プロジェクト登録画面が未実装のため、テスト本体を記述し、この振る舞いの実装に着手するとき解除します。")]
    public void 追加ボタンで開いたポップアップにプロジェクト名を入力して登録するとタブを表示します()
    {
    }

    [Fact(Skip = "プロジェクトごとの記録画面が未実装のため、テスト本体を記述し、この振る舞いの実装に着手するとき解除します。")]
    public void プロジェクトを指定せずに記録を開始できます()
    {
    }

    [Fact(Skip = "プロジェクトごとの記録画面が未実装のため、テスト本体を記述し、この振る舞いの実装に着手するとき解除します。")]
    public void 異なるプロジェクトの記録を同時に開始できます()
    {
    }

    [Fact(Skip = "プロジェクトごとの記録画面が未実装のため、テスト本体を記述し、この振る舞いの実装に着手するとき解除します。")]
    public void 同じ対象の記録を二重に開始できません()
    {
    }

    [Fact(Skip = "プロジェクトごとの記録画面が未実装のため、テスト本体を記述し、この振る舞いの実装に着手するとき解除します。")]
    public void 同時に記録中の各記録を独立して終了できます()
    {
    }

    public void Dispose()
    {
        context.Dispose();
        client.Dispose();
    }
}
