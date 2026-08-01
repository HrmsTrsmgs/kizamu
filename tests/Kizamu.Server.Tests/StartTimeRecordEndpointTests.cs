using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Server.Tests;

public sealed class StartTimeRecordEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    static readonly DateTimeOffset startedAt = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    readonly WebApplicationFactory<Program> configuredFactory;
    readonly HttpClient client;

    public StartTimeRecordEndpointTests(WebApplicationFactory<Program> factory)
    {
        configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(startedAt))));
        client = configuredFactory.CreateClient();
    }

    [Fact]
    public async Task 記録を開始すると識別子と開始時刻が返されます()
    {
        using var response = await client.PostAsync("/api/time-records/start", null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<StartTimeRecordResponse>();
        body.Should().NotBeNull();
        body.Id.Should().NotBe(Guid.Empty);
        body.StartedAt.Should().Be(startedAt);
    }

    public void Dispose()
    {
        client.Dispose();
        configuredFactory.Dispose();
    }

    sealed record StartTimeRecordResponse(Guid Id, DateTimeOffset StartedAt);

    sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
