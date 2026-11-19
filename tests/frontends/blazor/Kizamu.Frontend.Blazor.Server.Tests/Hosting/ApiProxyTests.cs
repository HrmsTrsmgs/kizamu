using System.Net;
using System.Text;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Yarp.ReverseProxy.Forwarder;

namespace Kizamu.Frontend.Blazor.Server.Tests.Hosting;

using static Kizamu.Tests.TestCancellation;

public sealed class ApiProxyTests : IDisposable
{
    readonly RecordingBackend backend = new();
    readonly WebApplicationFactory<global::Program> factory;
    readonly HttpClient client;

    public ApiProxyTests()
    {
        factory = CreateFactoryWithApiBackend("http://configured-backend/", backend);
        client = factory.CreateClient();
    }

    public static TheoryData<string, string, string?, HttpStatusCode> 中継する要求と応答 =>
    [
        ("GET", "/api/time-records?project=sample&limit=2", null, HttpStatusCode.OK),
        ("POST", "/api/time-records/start?source=blazor", "{\"projectId\":\"sample\"}", HttpStatusCode.Created),
        ("GET", "/api/does-not-exist", null, HttpStatusCode.NotFound),
    ];

    [Theory]
    [MemberData(nameof(中継する要求と応答))]
    public async Task 設定された業務APIへ元のパスとクエリを送ります(
        string method, string path, string? body, HttpStatusCode statusCode)
    {
        using var response = await 要求を送る(method, path, body, statusCode);

        backend.Uri.Should().Be(new Uri($"http://configured-backend{path}"));
    }

    [Theory]
    [MemberData(nameof(中継する要求と応答))]
    public async Task 元のHTTPメソッドを業務APIへ送ります(
        string method, string path, string? body, HttpStatusCode statusCode)
    {
        using var response = await 要求を送る(method, path, body, statusCode);

        backend.Method.Should().Be(method);
    }

    [Theory]
    [MemberData(nameof(中継する要求と応答))]
    public async Task 元の要求本文とそのメディアタイプを業務APIへ送ります(
        string method, string path, string? body, HttpStatusCode statusCode)
    {
        using var response = await 要求を送る(method, path, body, statusCode);

        backend.Body.Should().Be(body ?? "");
        if (body is not null)
        {
            backend.MediaType.Should().Be("application/json");
        }
    }

    [Theory]
    [MemberData(nameof(中継する要求と応答))]
    public async Task 業務APIのステータスコードを返します(
        string method, string path, string? body, HttpStatusCode statusCode)
    {
        using var response = await 要求を送る(method, path, body, statusCode);

        response.StatusCode.Should().Be(statusCode);
    }

    [Theory]
    [MemberData(nameof(中継する要求と応答))]
    public async Task 業務APIのJSON本文を同じメディアタイプで返します(
        string method, string path, string? body, HttpStatusCode statusCode)
    {
        using var response = await 要求を送る(method, path, body, statusCode);

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().Be("{\"id\":\"backend-response\"}");
    }

    async Task<HttpResponseMessage> 要求を送る(string method, string path, string? body, HttpStatusCode statusCode)
    {
        backend.StatusCode = statusCode;
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        backend.Dispose();
    }

    static WebApplicationFactory<global::Program> CreateFactoryWithApiBackend(
        string backendAddress, RecordingBackend backend) =>
        new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:backend:Destinations:backend:Address"] = backendAddress,
                }));
            builder.ConfigureServices(services => services.Replace(
                ServiceDescriptor.Singleton<IForwarderHttpClientFactory>(backend)));
        });

    sealed class RecordingBackend : HttpMessageHandler, IForwarderHttpClientFactory
    {
        public HttpStatusCode StatusCode { get; set; }
        public string? Method { get; set; }
        public Uri? Uri { get; set; }
        public string? Body { get; set; }
        public string? MediaType { get; set; }

        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) => new(this, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method.Method;
            Uri = request.RequestUri;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            MediaType = request.Content?.Headers.ContentType?.MediaType;
            return new(StatusCode)
            {
                Content = new StringContent("{\"id\":\"backend-response\"}", Encoding.UTF8, "application/json"),
            };
        }
    }
}
