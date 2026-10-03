using Kizamu.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Server.Endpoints.TimeRecords;

/// <summary>
/// APIが返す作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
/// <param name="EndedAt">作業記録を終了した時刻。記録中の場合はnull。</param>
sealed record TimeRecordResponse(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt)
{
    /// <summary>
    /// Domainの作業記録からAPIレスポンスを生成します。
    /// </summary>
    /// <param name="timeRecord">作業記録。</param>
    public TimeRecordResponse(TimeRecord timeRecord)
        : this(timeRecord.Id, timeRecord.StartedAt, timeRecord.EndedAt)
    {
    }

    /// <summary>
    /// 記録開始ユースケースの結果からAPIレスポンスを生成します。
    /// </summary>
    /// <param name="result">記録開始ユースケースの結果。</param>
    public TimeRecordResponse(StartTimeRecordResult result)
        : this(result.Id, result.StartedAt, null)
    {
    }

    /// <summary>
    /// 記録終了ユースケースの結果からAPIレスポンスを生成します。
    /// </summary>
    /// <param name="result">記録終了ユースケースの結果。</param>
    public TimeRecordResponse(EndTimeRecordResult result)
        : this(result.Id, result.StartedAt, result.EndedAt)
    {
    }
}
