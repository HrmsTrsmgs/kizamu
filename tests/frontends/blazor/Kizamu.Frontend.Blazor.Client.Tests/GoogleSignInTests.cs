using AwesomeAssertions;
using Bunit;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.Health;
using Kizamu.Frontend.Blazor.Presentation.Projects;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests;

public sealed class GoogleSignInTests : IDisposable
{
    readonly BunitContext context = new();
    readonly StubGoogleSignIn google = new();
    readonly IRenderedComponent<Client.App> tested;
    string? 記録通信に渡されたトークン;

    public GoogleSignInTests()
    {
        var 記録 = new TimeRecord(Guid.Parse("019cde5b-4c00-7000-8000-000000000001"), DateTimeOffset.Parse("2026-08-01T09:00:00Z"), null);
        context.Services.AddSingleton<Client.IGoogleSignIn>(google);
        context.Services.AddSingleton(new HealthStatusViewModel(new StubHealthService()));
        context.Services.AddSingleton<ITimeRecorderViewModel>(new TimeRecorderViewModel(new StubTimeRecordService(記録, 記録)));
        context.Services.AddSingleton(new ProjectTabsViewModel());
        context.Services.AddSingleton(TimeProvider.System);
        tested = context.Render<Client.App>(parameters => parameters.Add(it => it.CreateTimeRecords, token =>
        {
            記録通信に渡されたトークン = token;
            return new StubTimeRecordService(記録, 記録);
        }));
        google.OnCredential.Should().NotBeNull();
    }

    [Fact]
    public async Task Googleのログイン結果を受け取ると記録画面を表示します()
    {
        await tested.InvokeAsync(() => google.OnCredential?.Invoke("google-id-token"));

        tested.WaitForAssertion(() =>
        {
            var ボタンの表示名 =
                from button in tested.FindAll(".recorder-panel button")
                select button.TextContent;
            ボタンの表示名.Should().Contain("記録を開始");
        });
    }

    [Fact]
    public async Task Googleから受け取ったIDトークンを記録の通信サービスに渡します()
    {
        await tested.InvokeAsync(() => google.OnCredential?.Invoke("google-id-token"));

        記録通信に渡されたトークン.Should().Be("google-id-token");
    }

    public void Dispose() => context.Dispose();

    sealed class StubGoogleSignIn : Client.IGoogleSignIn
    {
        public Action<string>? OnCredential { get; private set; }

        public Task RenderButtonAsync(ElementReference container, Action<string> onCredential)
        {
            OnCredential = onCredential;
            return Task.CompletedTask;
        }
    }
}
