namespace Kizamu.Application.TimeRecords;

/// <summary>
/// 作業記録を開始します。
/// </summary>
/// <param name="timeProvider">開始時刻の取得元。</param>
public sealed class StartTimeRecord(TimeProvider timeProvider)
{
    /// <summary>
    /// 新しい作業記録の識別子と開始時刻を生成します。
    /// </summary>
    /// <returns>開始した作業記録。</returns>
    public StartTimeRecordResult Execute() =>
        new(Guid.NewGuid(), timeProvider.GetUtcNow());
}

/// <summary>
/// 開始した作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
public sealed record StartTimeRecordResult(Guid Id, DateTimeOffset StartedAt);
