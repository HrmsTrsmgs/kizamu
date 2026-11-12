using Kizamu.Frontend.Blazor.Presentation.Health;

namespace Kizamu.Frontend.Blazor.TestSupport;

/// <summary>接続状態のテストで、指定した応答を通信なしに返します。</summary>
sealed class StubHealthService : IHealthService
{
    public Task<bool> Response { get; set; } = Task.FromResult(true);

    public Task<bool> CheckHealthAsync() => Response;
}
