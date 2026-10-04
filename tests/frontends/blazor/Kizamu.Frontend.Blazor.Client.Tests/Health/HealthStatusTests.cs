using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Client.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests.Health;

public sealed class HealthStatusTests : IDisposable
{
    readonly BunitContext context = new();
    readonly TestHttpMessageHandler handler = new();
    readonly HttpClient client;

    public HealthStatusTests()
    {
        client = new(handler) { BaseAddress = new("http://localhost/") };
        context.Services.AddSingleton(new HealthApi(client));
    }

    [Fact]
    public void 正常な接続状態を表示します()
    {
        handler.Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { status = "ok" })
        });

        var tested = context.Render<HealthStatus>();

        tested.Find("p").TextContent.Should().Be("サーバー接続: 正常");
    }

    [Fact]
    public void サーバーの応答が正常でない場合は失敗を表示します()
    {
        handler.Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var tested = context.Render<HealthStatus>();

        tested.Find("p").TextContent.Should().Be("サーバー接続: 失敗");
    }

    [Fact]
    public void APIに接続できない場合は失敗を表示します()
    {
        handler.Respond = it => throw new HttpRequestException("offline");

        var tested = context.Render<HealthStatus>();

        tested.Find("p").TextContent.Should().Be("サーバー接続: 失敗");
    }

    [Fact]
    public void 応答待ちの間は確認中を表示します()
    {
        TaskCompletionSource<HttpResponseMessage> 応答 = new();
        handler.Respond = it => 応答.Task;

        var tested = context.Render<HealthStatus>();

        tested.Find("p").TextContent.Should().Be("サーバー接続: 確認中");
    }

    public void Dispose()
    {
        context.Dispose();
        client.Dispose();
    }
}
