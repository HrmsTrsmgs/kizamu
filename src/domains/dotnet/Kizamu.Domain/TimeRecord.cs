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
    /// 既存の作業記録の識別子と時刻から状態を復元します。
    /// </summary>
    /// <param name="id">既存の作業記録の識別子。</param>
    /// <param name="startedAt">記録された開始時刻。</param>
    /// <param name="endedAt">記録された終了時刻。記録中の場合はnull。</param>
    public TimeRecord(Guid id, DateTimeOffset startedAt, DateTimeOffset? endedAt)
        : this(id, startedAt)
    {
        EndedAt = endedAt;
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
    /// 作業記録を終了した時刻を取得します。
    /// </summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>
    /// 指定した時刻に開始した作業記録を生成します。
    /// </summary>
    /// <param name="startedAt">作業記録を開始した時刻。</param>
    /// <returns>開始状態の作業記録。</returns>
    public static TimeRecord Start(DateTimeOffset startedAt) =>
        new(Guid.NewGuid(), startedAt);

    /// <summary>
    /// 指定した時刻に作業記録を終了します。
    /// </summary>
    /// <param name="endedAt">作業記録を終了した時刻。</param>
    public void End(DateTimeOffset endedAt)
    {
        EndedAt = endedAt;
    }
}
