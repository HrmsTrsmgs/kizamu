namespace Kizamu.Application.TimeRecords;

/// <summary>
/// 作業記録を開始します。
/// </summary>
/// <param name="timeProvider">開始時刻の取得元。</param>
/// <param name="timeRecordState">作業記録の状態保存先。</param>
public sealed class StartTimeRecord(
    TimeProvider timeProvider,
    ITimeRecordState timeRecordState)
{
    /// <summary>
    /// 新しい作業記録の識別子と開始時刻を生成します。
    /// </summary>
    /// <returns>開始した作業記録。</returns>
    public StartTimeRecordResult Execute()
    {
        var result = new StartTimeRecordResult(Guid.NewGuid(), timeProvider.GetUtcNow());
        timeRecordState.StartedAt = result.StartedAt;
        return result;
    }
}

/// <summary>
/// 開始した作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
public sealed record StartTimeRecordResult(Guid Id, DateTimeOffset StartedAt);
