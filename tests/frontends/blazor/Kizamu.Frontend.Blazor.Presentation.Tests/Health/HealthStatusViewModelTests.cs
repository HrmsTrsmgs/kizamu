using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.Health;
using Kizamu.Frontend.Blazor.TestSupport;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.Health;

public sealed class HealthStatusViewModelTests
{
    readonly StubHealthService service = new();
    readonly HealthStatusViewModel tested;

    public HealthStatusViewModelTests() => tested = new(service);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task 確認した接続状態を公開する(bool 正常)
    {
        service.Response = Task.FromResult(正常);

        await tested.CheckHealthAsync();

        tested.IsHealthy.Should().Be(正常);
    }

    [Fact]
    public async Task 応答を待つ間は接続状態が未確定である()
    {
        var 応答 = new TaskCompletionSource<bool>();
        service.Response = 応答.Task;

        var 確認 = tested.CheckHealthAsync();

        tested.IsHealthy.Should().BeNull();
        応答.SetResult(true);
        await 確認;
    }

    [Fact]
    public async Task 接続状態を確認できない場合は失敗として扱う()
    {
        service.Response = Task.FromException<bool>(new InvalidOperationException());

        await tested.CheckHealthAsync();

        tested.IsHealthy.Should().BeFalse();
    }
}
