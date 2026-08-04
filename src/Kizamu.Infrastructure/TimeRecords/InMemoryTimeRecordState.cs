using Kizamu.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Infrastructure.TimeRecords;

/// <summary>
/// インスタンスの存続中、作業記録の状態をメモリ上に保持します。
/// </summary>
public sealed class InMemoryTimeRecordState : ITimeRecordState
{
    TimeRecord? addedTimeRecord;

    /// <inheritdoc />
    public TimeRecord? Current { get; set; }

    /// <summary>
    /// 作業記録を追加します。
    /// </summary>
    /// <param name="timeRecord">追加する作業記録。</param>
    public void Add(TimeRecord timeRecord)
    {
        addedTimeRecord = timeRecord;
    }

    /// <summary>
    /// 追加したすべての作業記録を取得します。
    /// </summary>
    /// <returns>追加した作業記録。</returns>
    public IReadOnlyList<TimeRecord> All =>
        addedTimeRecord is null ? [] : [addedTimeRecord];
}
