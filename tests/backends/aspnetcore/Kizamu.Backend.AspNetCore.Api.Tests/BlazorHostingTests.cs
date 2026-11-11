using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kizamu.Backend.AspNetCore.Api.Tests;

using static Kizamu.Tests.TestCancellation;

public sealed class BlazorHostingTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("/")]
    [InlineData("/app.css")]
    [InlineData("/_framework/blazor.webassembly.js")]
    public async Task 業務APIサーバーはBlazor画面を配信せず404を返します(string path)
    {
        using var response = await client.GetAsync(path, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose() => client.Dispose();
}
