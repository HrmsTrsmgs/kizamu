using AwesomeAssertions;
using Bunit;
using Kizamu.Frontend.Blazor.Client.Health;
using Kizamu.Frontend.Blazor.Presentation.Health;
using Kizamu.Frontend.Blazor.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Kizamu.Frontend.Blazor.Client.Tests.Health;

public sealed class HealthStatusTests : IDisposable
{
    readonly BunitContext context = new();
    readonly StubHealthService service = new();

    public HealthStatusTests() => context.Services.AddSingleton(new HealthStatusViewModel(service));

    [Theory]
    [InlineData(true, "正常")]
    [InlineData(false, "失敗")]
    public async Task 応答待ちの表示から確認結果の日本語表示に切り替わります(bool 正常, string 表示)
    {
        var 応答 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.Response = 応答.Task;

        var tested = context.Render<HealthStatus>();

        tested.Find("p[aria-live='polite']").TextContent.Should().Be("サーバー接続: 確認中");
        応答.SetResult(正常);
        await tested.WaitForAssertionAsync(() =>
            tested.Find("p[aria-live='polite']").TextContent.Should().Be($"サーバー接続: {表示}"));
    }

    public void Dispose() => context.Dispose();
}
