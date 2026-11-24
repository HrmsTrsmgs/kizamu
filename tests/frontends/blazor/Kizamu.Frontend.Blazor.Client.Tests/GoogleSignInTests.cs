using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.Health;
using Kizamu.Frontend.Blazor.Presentation.Projects;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests;

public sealed class GoogleSignInTests : IDisposable
{
    readonly BunitContext context = new();
    readonly StubGoogleSignIn google = new();
    readonly TestHttpMessageHandler handler;
    readonly HttpClient client;
    string? 一覧要求の認証情報;

    public GoogleSignInTests()
    {
        handler = new() { Respond = 一覧要求の認証情報を記録して空の一覧を返す };
        client = new(handler) { BaseAddress = new("http://localhost/") };
        context.Services.AddSingleton(client);
        context.Services.AddSingleton<Client.IGoogleSignIn>(google);
        context.Services.AddSingleton(new HealthStatusViewModel(new StubHealthService()));
        context.Services.AddSingleton(new ProjectTabsViewModel());
        context.Services.AddSingleton(TimeProvider.System);
    }

    [Fact]
    public async Task Googleのログイン結果を受け取ると記録画面を表示します()
    {
        var tested = context.Render<Client.App>();

        await tested.InvokeAsync(() => google.SignIn("google-id-token"));

        tested.WaitForAssertion(() =>
        {
            var ボタンの表示名 =
                from button in tested.FindAll(".recorder-panel button")
                select button.TextContent;
            ボタンの表示名.Should().Contain("記録を開始");
        });
    }

    [Fact]
    public async Task Googleの認証コールバックで受け取ったIDトークンを記録サービスの生成処理に渡します()
    {
        string? 記録通信に渡されたトークン = null;
        var tested = 記録サービスの生成を差し替えて画面を表示する(token => 記録通信に渡されたトークン = token);

        await tested.InvokeAsync(() => google.SignIn("google-id-token"));

        記録通信に渡されたトークン.Should().Be("google-id-token");
    }

    [Fact]
    public async Task Google認証後の初回一覧取得では受け取ったIDトークンをBearer認証で送ります()
    {
        var tested = context.Render<Client.App>();

        await tested.InvokeAsync(() => google.SignIn("google-id-token"));

        tested.WaitForAssertion(() =>
        {
            handler.Requests.Should().Equal((HttpMethod.Get, "/api/time-records"));
            一覧要求の認証情報.Should().Be("Bearer google-id-token");
        });
    }

    [Fact]
    public async Task ログアウトするとGoogleのログインボタンを再描画します()
    {
        var tested = context.Render<Client.App>();
        await tested.InvokeAsync(() => google.SignIn("google-id-token"));
        google.RenderCount.Should().Be(1);

        tested.FindAll("button").Single(button => button.TextContent == "ログアウト").Click();

        tested.WaitForAssertion(() => google.RenderCount.Should().Be(2));
    }

    IRenderedComponent<Client.App> 記録サービスの生成を差し替えて画面を表示する(Action<string> トークンを記録する)
    {
        var 記録 = new TimeRecord(Guid.Empty, DateTimeOffset.UnixEpoch, null);
        return context.Render<Client.App>(parameters => parameters.Add(it => it.CreateTimeRecords, token =>
        {
            トークンを記録する(token);
            return new StubTimeRecordService(記録, 記録);
        }));
    }

    Task<HttpResponseMessage> 一覧要求の認証情報を記録して空の一覧を返す(HttpRequestMessage request)
    {
        一覧要求の認証情報 = request.Headers.Authorization?.ToString();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<object>()) });
    }

    public void Dispose()
    {
        context.Dispose();
        client.Dispose();
    }

    sealed class StubGoogleSignIn : Client.IGoogleSignIn
    {
        Action<string>? onCredential;

        public int RenderCount { get; private set; }

        public void SignIn(string token)
        {
            onCredential.Should().NotBeNull();
            onCredential(token);
        }

        public Task RenderButtonAsync(ElementReference container, Action<string> onCredential)
        {
            RenderCount++;
            this.onCredential = onCredential;
            return Task.CompletedTask;
        }
    }
}
