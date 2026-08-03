using Kizamu.Application.TimeRecords;

namespace Kizamu.Server.Endpoints.TimeRecords;

/// <summary>
/// 記録開始APIが返す作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
sealed record StartTimeRecordResponse(Guid Id, DateTimeOffset StartedAt)
{
    /// <summary>
    /// 記録開始ユースケースの結果からAPIレスポンスを生成します。
    /// </summary>
    /// <param name="result">記録開始ユースケースの結果。</param>
    public StartTimeRecordResponse(StartTimeRecordResult result)
        : this(result.Id, result.StartedAt)
    {
    }
}
