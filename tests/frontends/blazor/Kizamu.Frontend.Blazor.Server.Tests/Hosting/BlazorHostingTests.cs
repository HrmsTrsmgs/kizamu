using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kizamu.Frontend.Blazor.Server.Tests.Hosting;

using static Kizamu.Tests.TestCancellation;

public sealed class BlazorHostingTests(WebApplicationFactory<global::Program> factory)
    : IClassFixture<WebApplicationFactory<global::Program>>, IDisposable
{
    readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task サーバーのルートでBlazor画面の起動HTMLを配信します()
    {
        using var response = await client.GetAsync("/", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().Contain("_framework/blazor.webassembly.js");
    }

    [Theory]
    [InlineData("/app.css", "text/css")]
    [InlineData("/_framework/blazor.webassembly.js", "text/javascript")]
    public async Task Blazor画面に必要な静的ファイルを配信します(string path, string mediaType)
    {
        using var response = await client.GetAsync(path, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(mediaType);
    }

    public void Dispose() => client.Dispose();
}
