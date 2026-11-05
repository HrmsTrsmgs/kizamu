using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kizamu.Backend.AspNetCore.Api.Tests;

public sealed class BlazorHostingTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    readonly HttpClient client;

    public BlazorHostingTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/app.css")]
    [InlineData("/_framework/blazor.webassembly.js")]
    public async Task 業務APIサーバーはBlazor画面を配信せず404を返します(string path)
    {
        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose() => client.Dispose();
}
