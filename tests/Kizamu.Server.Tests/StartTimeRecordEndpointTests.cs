using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Server.Tests;

public sealed class StartTimeRecordEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    readonly FakeTimeProvider timeProvider;
    readonly WebApplicationFactory<Program> configuredFactory;
    readonly HttpClient client;

    public StartTimeRecordEndpointTests(WebApplicationFactory<Program> factory)
    {
        timeProvider = new FakeTimeProvider();
        configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton<TimeProvider>(timeProvider)));
        client = configuredFactory.CreateClient();
    }

    public static TheoryData<DateTimeOffset> 開始時刻の例 => new()
    {
        new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
        new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero),
    };

    [Theory]
    [MemberData(nameof(開始時刻の例))]
    public async Task 記録を開始すると識別子と開始時刻が返されます(DateTimeOffset startedAt)
    {
        timeProvider.SetUtcNow(startedAt);

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
}
