using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kizamu.Backend.AspNetCore.Api.Tests;

using static Kizamu.Tests.TestCancellation;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    readonly HttpClient client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task 正常性を確認すると正常な状態が返されます()
    {
        using var response = await client.GetAsync("/api/health", CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body.Status.Should().Be("ok");
    }

    public void Dispose() => client.Dispose();

    sealed record HealthResponse(string Status);
}
