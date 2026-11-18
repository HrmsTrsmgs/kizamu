using System.Net;
using System.Net.Http.Json;

namespace Kizamu.Frontend.Blazor.TestSupport;

sealed class TestHttpMessageHandler : HttpMessageHandler
{
    public List<(HttpMethod Method, string Path)> Requests { get; } = [];
    public Func<HttpRequestMessage, Task<HttpResponseMessage>> Respond { get; set; } =
        _ => JsonResponse(Array.Empty<object>(), HttpStatusCode.OK);

    /// <summary>各要求に対して指定した内容のJSON応答を新しく返します。</summary>
    public void RespondWithJson<T>(T body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        Respond = _ => JsonResponse(body, statusCode);

    static Task<HttpResponseMessage> JsonResponse<T>(T body, HttpStatusCode statusCode) =>
        Task.FromResult(new HttpResponseMessage(statusCode) { Content = JsonContent.Create(body) });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request.Method, request.RequestUri?.AbsolutePath ?? ""));
        return Respond(request);
    }
}
