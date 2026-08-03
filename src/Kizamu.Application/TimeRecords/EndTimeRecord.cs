namespace Kizamu.Application.TimeRecords;

/// <summary>
/// 作業記録を終了します。
/// </summary>
/// <param name="timeProvider">終了時刻の取得元。</param>
/// <param name="timeRecordState">作業記録の状態取得元。</param>
public sealed class EndTimeRecord(
    TimeProvider timeProvider,
    ITimeRecordState timeRecordState)
{
    /// <summary>
    /// 指定した作業記録の終了結果を生成します。
    /// </summary>
    /// <param name="id">作業記録の識別子。</param>
    /// <returns>終了した作業記録。</returns>
    public EndTimeRecordResult Execute(Guid id)
    {
        var timeRecord = timeRecordState.Current;
        var endedAt = timeProvider.GetUtcNow();
        if (timeRecord is null)
        {
            return new(id, default, endedAt);
        }

        timeRecord.End(endedAt);
        return new(id, timeRecord.StartedAt, endedAt);
    }
}

/// <summary>
/// 終了した作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
/// <param name="EndedAt">作業記録を終了した時刻。</param>
public sealed record EndTimeRecordResult(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt);
