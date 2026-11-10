using System.Net;
using System.Net.Http.Json;

namespace Kizamu.Frontend.Blazor.TestSupport;

sealed class TestHttpMessageHandler : HttpMessageHandler
{
    public List<(HttpMethod Method, string Path)> Requests { get; } = [];
    public Func<HttpRequestMessage, Task<HttpResponseMessage>> Respond { get; set; } =
        it => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<object>()) });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request.Method, request.RequestUri?.AbsolutePath ?? ""));
        return Respond(request);
    }
}
