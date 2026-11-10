using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Application.TimeRecords;

/// <summary>
/// 作業記録を開始します。
/// </summary>
/// <param name="timeProvider">開始時刻の取得元。</param>
/// <param name="timeRecordState">作業記録の状態保存先。</param>
public sealed class TimeRecordStartOperation(
    TimeProvider timeProvider,
    ITimeRecordState timeRecordState)
{
    /// <summary>
    /// 新しい作業記録の識別子と開始時刻を生成します。
    /// </summary>
    /// <returns>開始した作業記録。</returns>
    public StartTimeRecordResult Execute()
    {
        var timeRecord = TimeRecord.Start(timeProvider.GetUtcNow());
        timeRecordState.Add(timeRecord);
        return new(timeRecord);
    }
}

/// <summary>
/// 開始した作業記録を表します。
/// </summary>
public sealed record StartTimeRecordResult
{
    /// <summary>
    /// Domainの作業記録から開始結果を生成します。
    /// </summary>
    /// <param name="timeRecord">開始した作業記録。</param>
    public StartTimeRecordResult(TimeRecord timeRecord)
    {
        Id = timeRecord.Id;
        StartedAt = timeRecord.StartedAt;
    }

    /// <summary>
    /// 作業記録の識別子を取得します。
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 作業記録を開始した時刻を取得します。
    /// </summary>
    public DateTimeOffset StartedAt { get; }
}
