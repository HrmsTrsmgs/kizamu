using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;

/// <summary>作業記録のHTTP APIを呼び出します。HttpClientの所有権は呼び出し側にあります。</summary>
/// <param name="client">APIと同じオリジンを基底URIとするHTTPクライアント。</param>
public sealed partial class TimeRecordApi(HttpClient client) : ITimeRecordService
{
    /// <summary>サーバーが保持している作業記録を取得します。</summary>
    /// <returns>記録中と終了済みの記録を含む一覧。</returns>
    public async Task<ImmutableArray<TimeRecord>> GetTimeRecordsAsync() =>
        [
            .. (await client.GetFromJsonAsync("api/time-records", TimeRecordJsonContext.Default.ImmutableArrayTimeRecordResponse))
                .Select(it => it.ToDomain())
        ];

    /// <summary>プロジェクト未指定の記録を開始します。</summary>
    /// <returns>サーバーが確定した記録と開始時刻。</returns>
    public Task<TimeRecord> StartTimeRecordAsync() => PostTimeRecordAsync("api/time-records/start");

    /// <summary>指定した記録を終了します。</summary>
    /// <param name="id">終了する記録の識別子。</param>
    /// <returns>サーバーが確定した終了時刻を含む記録。</returns>
    public Task<TimeRecord> EndTimeRecordAsync(Guid id) => PostTimeRecordAsync($"api/time-records/{id}/end");

    /// <summary>開始・終了で共通する応答の読み取りとHTTP応答の破棄を一か所で行います。</summary>
    async Task<TimeRecord> PostTimeRecordAsync(string endpoint)
    {
        using var response = await client.PostAsync(endpoint, null);
        return (await response.Content.ReadFromJsonAsync(TimeRecordJsonContext.Default.TimeRecordResponse)).ToDomain();
    }

    /// <summary>公開時のコード縮小でも、応答の読み取りに必要な型情報を保持します。</summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ImmutableArray<TimeRecordResponse>))]
    [JsonSerializable(typeof(TimeRecordResponse))]
    partial class TimeRecordJsonContext : JsonSerializerContext;
}
