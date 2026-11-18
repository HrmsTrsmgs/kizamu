using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;

namespace Kizamu.Frontend.Blazor.Infrastructure.Tests.TimeRecords;

public sealed class TimeRecordAuthorizationTests
{
    [Theory]
    [InlineData("list")]
    [InlineData("start")]
    [InlineData("end")]
    public async Task 利用可能な認証トークンを記録APIへ送ります(string operation)
    {
        var handler = new TestHttpMessageHandler();
        string? authorization = null;
        handler.Respond = request =>
        {
            authorization = request.Headers.Authorization?.ToString();
            object body = operation == "list"
                ? Array.Empty<object>()
                : new { id = Guid.Empty, startedAt = "2026-08-01T09:00:00Z", endedAt = (string?)null };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        };
        using var client = new HttpClient(handler) { BaseAddress = new("http://localhost/") };
        var tested = new TimeRecordApi(client, "id-token");

        switch (operation)
        {
            case "list": await tested.GetTimeRecordsAsync(); break;
            case "start": await tested.StartTimeRecordAsync(); break;
            case "end": await tested.EndTimeRecordAsync(Guid.Empty); break;
        }

        authorization.Should().Be("Bearer id-token");
    }
}
