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

public sealed class ApiProxyTests
{
    [Theory]
    [InlineData("GET", "/api/time-records?project=sample&limit=2", null, HttpStatusCode.OK)]
    [InlineData("POST", "/api/time-records/start?source=blazor", "{\"projectId\":\"sample\"}", HttpStatusCode.Created)]
    [InlineData("GET", "/api/does-not-exist", null, HttpStatusCode.NotFound)]
    public async Task 設定された業務APIへ要求を中継してステータスとJSONを返します(
        string method, string path, string? body, HttpStatusCode statusCode)
    {
        using var backend = new RecordingBackend(statusCode);
        using var factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:backend:Destinations:backend:Address"] = "http://configured-backend/",
                }));
            builder.ConfigureServices(services => services.Replace(
                ServiceDescriptor.Singleton<IForwarderHttpClientFactory>(backend)));
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(statusCode);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        (await response.Content.ReadAsStringAsync()).Should().Be("{\"id\":\"backend-response\"}");
        backend.Method.Should().Be(method);
        backend.Uri.Should().Be(new Uri($"http://configured-backend{path}"));
        backend.Body.Should().Be(body ?? "");
        if (body is not null)
        {
            backend.MediaType.Should().Be("application/json");
        }
    }

    sealed class RecordingBackend(HttpStatusCode statusCode) : HttpMessageHandler, IForwarderHttpClientFactory
    {
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
            return new(statusCode)
            {
                Content = new StringContent("{\"id\":\"backend-response\"}", Encoding.UTF8, "application/json"),
            };
        }
    }
}
