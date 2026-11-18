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

public sealed class GoogleSignInTests
{
    [Fact]
    public async Task Googleのログイン結果を記録画面へ渡します()
    {
        using var context = new BunitContext();
        var google = new StubGoogleSignIn();
        var 記録 = new TimeRecord(Guid.Parse("019cde5b-4c00-7000-8000-000000000001"), DateTimeOffset.Parse("2026-08-01T09:00:00Z"), null);
        context.Services.AddSingleton<Client.IGoogleSignIn>(google);
        context.Services.AddSingleton(new HealthStatusViewModel(new StubHealthService()));
        context.Services.AddSingleton<ITimeRecorderViewModel>(new TimeRecorderViewModel(new StubTimeRecordService(記録, 記録)));
        context.Services.AddSingleton(new ProjectTabsViewModel());
        context.Services.AddSingleton(TimeProvider.System);
        string? 記録通信に渡されたトークン = null;
        var tested = context.Render<Client.App>(parameters => parameters.Add(it => it.CreateTimeRecords, token =>
        {
            記録通信に渡されたトークン = token;
            return new StubTimeRecordService(記録, 記録);
        }));

        google.OnCredential.Should().NotBeNull();
        await tested.InvokeAsync(() => google.OnCredential?.Invoke("google-id-token"));

        tested.WaitForAssertion(() => tested.FindAll(".recorder-panel button").Select(it => it.TextContent).Should().Contain("記録を開始"));
        記録通信に渡されたトークン.Should().Be("google-id-token");
    }

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
