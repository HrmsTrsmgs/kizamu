using Kizamu.Domain;

namespace Kizamu.Application.TimeRecords;

/// <summary>
/// API呼び出しをまたいで保持する作業記録の状態を表します。
/// </summary>
public interface ITimeRecordState
{
    /// <summary>
    /// 最後に追加した作業記録を取得します。
    /// </summary>
    TimeRecord? Current { get; }

    /// <summary>
    /// 保持しているすべての作業記録を取得します。
    /// </summary>
    IReadOnlyList<TimeRecord> All { get; }

    /// <summary>
    /// 作業記録を追加します。
    /// </summary>
    /// <param name="timeRecord">追加する作業記録。</param>
    void Add(TimeRecord timeRecord);
}
