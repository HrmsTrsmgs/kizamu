namespace Kizamu.Backend.AspNetCore.Application.TimeRecords;

/// <summary>
/// 作業記録を終了します。
/// </summary>
/// <param name="timeProvider">終了時刻の取得元。</param>
/// <param name="timeRecordState">作業記録の保存先。</param>
public sealed class TimeRecordEndOperation(
    TimeProvider timeProvider,
    ITimeRecordState timeRecordState)
{
    /// <summary>
    /// 指定した作業記録を終了して保存します。
    /// </summary>
    /// <param name="id">作業記録の識別子。</param>
    /// <returns>終了した作業記録。</returns>
    /// <exception cref="TimeRecordNotFoundException">指定した作業記録が保存先に存在しない場合。</exception>
    public EndTimeRecordResult Execute(Guid id)
    {
        var endedAt = timeProvider.GetUtcNow();
        var timeRecord = timeRecordState.Find(id) ?? throw new TimeRecordNotFoundException();

        var endedTimeRecord = timeRecord.End(endedAt);
        timeRecordState.Update(endedTimeRecord);
        return new(endedTimeRecord.Id, endedTimeRecord.StartedAt, endedAt);
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
