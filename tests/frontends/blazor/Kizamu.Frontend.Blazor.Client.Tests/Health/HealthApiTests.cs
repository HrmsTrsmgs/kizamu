using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Client.Health;

namespace Kizamu.Frontend.Blazor.Client.Tests.Health;

public sealed class HealthApiTests : IDisposable
{
    readonly TestHttpMessageHandler handler = new();
    readonly HttpClient client;
    readonly HealthApi tested;

    public HealthApiTests()
    {
        client = new(handler) { BaseAddress = new("http://localhost/") };
        tested = new(client);
    }

    [Fact]
    public async Task 正常性確認APIへGETします()
    {
        応答する(new { status = "ok" });

        await tested.CheckHealthAsync();

        handler.Requests.Should().Equal((HttpMethod.Get, "/api/health"));
    }

    [Fact]
    public async Task 正常な応答ではtrueを返します()
    {
        応答する(new { status = "ok" });

        (await tested.CheckHealthAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task 正常でないHTTP応答ではfalseを返します()
    {
        handler.Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        (await tested.CheckHealthAsync()).Should().BeFalse();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"status\":\"error\"}")]
    [InlineData("null")]
    public async Task 正常でない応答内容ではfalseを返します(string json)
    {
        handler.Respond = it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });

        (await tested.CheckHealthAsync()).Should().BeFalse();
    }

    void 応答する<T>(T body) => handler.Respond = it =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });

    public void Dispose() => client.Dispose();
}
