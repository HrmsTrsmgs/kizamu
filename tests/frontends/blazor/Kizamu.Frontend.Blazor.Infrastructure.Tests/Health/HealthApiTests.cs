using System.Net;
using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Infrastructure.Health;
using Kizamu.Frontend.Blazor.TestSupport;

namespace Kizamu.Frontend.Blazor.Infrastructure.Tests.Health;

public sealed class HealthApiTests : IDisposable
{
    readonly TestHttpMessageHandler handler = new();
    readonly HttpClient client;
    readonly HealthApi tested;

    public HealthApiTests()
    {
        client = new(handler) { BaseAddress = new("http://localhost/") };
        tested = new(client);
        handler.RespondWithJson(new { status = "ok" });
    }

    [Fact]
    public async Task サーバーの接続確認では正常性確認APIへGET要求を送ります()
    {
        await tested.CheckHealthAsync();

        handler.Requests.Should().Equal((HttpMethod.Get, "/api/health"));
    }

    [Fact]
    public async Task 正常性確認APIが成功し本文のstatusがokなら接続確認はtrueを返します()
    {
        (await tested.CheckHealthAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task 正常性確認APIがHTTPエラーを返すと接続確認はfalseを返します()
    {
        handler.Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        (await tested.CheckHealthAsync()).Should().BeFalse();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"status\":\"error\"}")]
    [InlineData("null")]
    public async Task 正常性確認APIが成功しても本文のstatusがokでなければ接続確認はfalseを返します(string json)
    {
        handler.Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

        (await tested.CheckHealthAsync()).Should().BeFalse();
    }

    public void Dispose() => client.Dispose();
}
