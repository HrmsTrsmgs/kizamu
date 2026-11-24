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
        return TimeRecordResultMapper.ToStartResult(timeRecord);
    }
}

/// <summary>
/// 開始した作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
public sealed record StartTimeRecordResult(
    Guid Id,
    DateTimeOffset StartedAt);
