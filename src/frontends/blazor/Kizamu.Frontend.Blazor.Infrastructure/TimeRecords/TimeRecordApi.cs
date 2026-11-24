using System.Collections.Immutable;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;

/// <summary>作業記録のHTTP APIを呼び出します。HttpClientの所有権は呼び出し側にあります。</summary>
/// <param name="client">APIと同じオリジンを基底URIとするHTTPクライアント。</param>
/// <param name="token">APIへ送信する認証トークン。</param>
public sealed partial class TimeRecordApi(HttpClient client, string? token = null) : ITimeRecordService
{
    /// <summary>サーバーが保持している作業記録を取得します。</summary>
    /// <returns>記録中と終了済みの記録を含む一覧。</returns>
    public async Task<ImmutableArray<TimeRecord>> GetTimeRecordsAsync()
    {
        var responseRecords = await RequestJsonAsync(
            HttpMethod.Get, "api/time-records", TimeRecordJsonContext.Default.ImmutableArrayTimeRecordResponse);
        var records =
            from record in responseRecords
            select TimeRecordResponseMapper.ToDomain(record);
        return [.. records];
    }

    /// <summary>プロジェクト未指定の記録を開始します。</summary>
    /// <returns>サーバーが確定した記録と開始時刻。</returns>
    public async Task<TimeRecord> StartTimeRecordAsync() =>
        TimeRecordResponseMapper.ToDomain(await PostTimeRecordAsync("api/time-records/start"));

    /// <summary>指定した記録を終了します。</summary>
    /// <param name="id">終了する記録の識別子。</param>
    /// <returns>サーバーが確定した終了時刻を含む記録。</returns>
    /// <exception cref="InvalidDataException">成功応答が指定した記録と一致しないか、終了時刻を含まない場合。</exception>
    public async Task<TimeRecord> EndTimeRecordAsync(Guid id)
    {
        var record = await PostTimeRecordAsync($"api/time-records/{id}/end");
        if (record.Id != id || record.EndedAt is null)
            throw new InvalidDataException("終了応答に指定した記録の終了時刻が含まれていません。");
        return TimeRecordResponseMapper.ToDomain(record);
    }

    Task<TimeRecordResponse> PostTimeRecordAsync(string endpoint) =>
        RequestJsonAsync(HttpMethod.Post, endpoint, TimeRecordJsonContext.Default.TimeRecordResponse);

    async Task<T?> RequestJsonAsync<T>(HttpMethod method, string endpoint, JsonTypeInfo<T> jsonTypeInfo)
    {
        using var request = CreateRequest(method, endpoint);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(jsonTypeInfo);
    }

    HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    /// <summary>公開時のコード縮小でも、応答の読み取りに必要な型情報を保持します。</summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true, GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(ImmutableArray<TimeRecordResponse>))]
    [JsonSerializable(typeof(TimeRecordResponse))]
    partial class TimeRecordJsonContext : JsonSerializerContext;
}
