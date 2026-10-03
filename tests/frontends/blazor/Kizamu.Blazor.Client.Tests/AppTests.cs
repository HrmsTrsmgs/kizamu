using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bunit;
using Kizamu.Blazor.Client.Health;
using Kizamu.Blazor.Client.TimeRecords;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Blazor.Client.Tests;

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
        context.Services.AddSingleton(new TimeRecordApi(client));
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

    public void Dispose()
    {
        context.Dispose();
        client.Dispose();
    }
}
