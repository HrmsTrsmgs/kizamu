using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Kizamu.Blazor.Client.Health;

/// <summary>サーバーの正常性を確認します。HttpClientは呼び出し側が管理します。</summary>
/// <param name="client">サーバーを基底URIとするHTTPクライアント。</param>
public sealed partial class HealthApi(HttpClient client)
{
    /// <summary>正常性確認APIが正常な状態を返すか確認します。</summary>
    /// <returns>HTTP応答が成功し、statusがokの場合にtrue。</returns>
    public async Task<bool> CheckHealthAsync()
    {
        using var response = await client.GetAsync("api/health");
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        return (await response.Content.ReadFromJsonAsync(HealthJsonContext.Default.HealthResponse))?.Status == "ok";
    }

    /// <summary>statusの欠落を正常と扱わないため、nullableで読み取ります。</summary>
    sealed record HealthResponse(string? Status);

    /// <summary>ブラウザへ公開する際にリフレクションへ依存しない読み取りを生成します。</summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(HealthResponse))]
    partial class HealthJsonContext : JsonSerializerContext;
}
