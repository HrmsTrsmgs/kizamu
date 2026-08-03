using Kizamu.Application.TimeRecords;

namespace Kizamu.Server.Endpoints.TimeRecords;

/// <summary>
/// 記録終了APIが返す作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
/// <param name="EndedAt">作業記録を終了した時刻。</param>
sealed record EndTimeRecordResponse(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt)
{
    /// <summary>
    /// 記録終了ユースケースの結果からAPIレスポンスを生成します。
    /// </summary>
    /// <param name="result">記録終了ユースケースの結果。</param>
    public EndTimeRecordResponse(EndTimeRecordResult result)
        : this(result.Id, result.StartedAt, result.EndedAt)
    {
    }
}
