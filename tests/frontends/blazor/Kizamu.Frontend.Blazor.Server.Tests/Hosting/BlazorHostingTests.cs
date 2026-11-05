using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kizamu.Frontend.Blazor.Server.Tests.Hosting;

public sealed class BlazorHostingTests : IClassFixture<WebApplicationFactory<global::Program>>, IDisposable
{
    readonly HttpClient client;

    public BlazorHostingTests(WebApplicationFactory<global::Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task サーバーのルートでBlazor画面の起動HTMLを配信します()
    {
        using var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        (await response.Content.ReadAsStringAsync()).Should().Contain("_framework/blazor.webassembly.js");
    }

    [Theory]
    [InlineData("/app.css", "text/css")]
    [InlineData("/_framework/blazor.webassembly.js", "text/javascript")]
    public async Task Blazor画面に必要な静的ファイルを配信します(string path, string mediaType)
    {
        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(mediaType);
    }

    public void Dispose() => client.Dispose();
}
