namespace Kizamu.Frontend.Blazor.Presentation.Health;

/// <summary>サーバー接続の確認結果を保持します。</summary>
/// <param name="service">サーバーの接続を確認するサービス。</param>
public sealed class HealthStatusViewModel(IHealthService service)
{
    /// <summary>正常かどうかを取得します。応答待ちの間はnullです。</summary>
    public bool? IsHealthy { get; private set; }

    /// <summary>接続を確認し、応答が得られなかった場合も失敗として扱います。</summary>
    public async Task CheckHealthAsync()
    {
        try
        {
            IsHealthy = await service.CheckHealthAsync();
        }
        catch
        {
            IsHealthy = false;
        }
    }
}
