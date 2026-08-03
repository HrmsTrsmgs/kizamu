namespace Kizamu.Domain;

/// <summary>
/// 作業時間の記録を表します。
/// </summary>
public sealed class TimeRecord
{
    TimeRecord(Guid id, DateTimeOffset startedAt)
    {
        Id = id;
        StartedAt = startedAt;
    }

    /// <summary>
    /// 作業記録の識別子を取得します。
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 作業記録を開始した時刻を取得します。
    /// </summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>
    /// 指定した時刻に開始した作業記録を生成します。
    /// </summary>
    /// <param name="startedAt">作業記録を開始した時刻。</param>
    /// <returns>開始状態の作業記録。</returns>
    public static TimeRecord Start(DateTimeOffset startedAt) =>
        new(Guid.NewGuid(), startedAt);
}
