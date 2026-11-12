namespace Kizamu.Frontend.Blazor.Presentation.Health;

/// <summary>サーバーの接続状態を確認する操作を提供します。</summary>
public interface IHealthService
{
    /// <summary>サーバーが正常な状態を返すか確認します。</summary>
    /// <returns>正常な場合にtrue。</returns>
    Task<bool> CheckHealthAsync();
}
