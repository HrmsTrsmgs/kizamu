using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;

namespace Kizamu.Frontend.Blazor.Infrastructure.Tests.TimeRecords;

public sealed class TimeRecordAuthorizationTests : IDisposable
{
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");
    static readonly DateTimeOffset 終了時刻 = DateTimeOffset.Parse("2026-08-01T10:30:00Z");
    readonly HttpClient client;
    readonly TimeRecordApi tested;
    object responseBody = Array.Empty<TimeRecordResponse>();
    string? authorization;

    public TimeRecordAuthorizationTests()
    {
        var handler = new TestHttpMessageHandler { Respond = Respond };
        client = new(handler) { BaseAddress = new("http://localhost/") };
        tested = new(client, "id-token");
    }

    [Fact]
    public async Task 記録の一覧取得では指定したIDトークンをBearer認証で送ります()
    {
        await tested.GetTimeRecordsAsync();

        authorization.Should().Be("Bearer id-token");
    }

    [Fact]
    public async Task 記録の開始では指定したIDトークンをBearer認証で送ります()
    {
        responseBody = new TimeRecordResponse(Guid.Empty, 開始時刻, null);

        await tested.StartTimeRecordAsync();

        authorization.Should().Be("Bearer id-token");
    }

    [Fact]
    public async Task 記録の終了では指定したIDトークンをBearer認証で送ります()
    {
        responseBody = new TimeRecordResponse(Guid.Empty, 開始時刻, 終了時刻);

        await tested.EndTimeRecordAsync(Guid.Empty);

        authorization.Should().Be("Bearer id-token");
    }

    Task<HttpResponseMessage> Respond(HttpRequestMessage request)
    {
        authorization = request.Headers.Authorization?.ToString();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(responseBody) });
    }

    public void Dispose() => client.Dispose();
}
