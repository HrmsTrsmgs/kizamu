using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

/// <summary>
/// インスタンスの存続中、作業記録の状態をメモリ上に保持します。
/// </summary>
public sealed class InMemoryTimeRecordState : ITimeRecordState
{
    readonly List<TimeRecord> timeRecords = [];

    /// <inheritdoc />
    public TimeRecord? Current => timeRecords.LastOrDefault();

    /// <summary>
    /// 作業記録を追加します。
    /// </summary>
    /// <param name="timeRecord">追加する作業記録。</param>
    public void Add(TimeRecord timeRecord)
    {
        timeRecords.Add(timeRecord);
    }

    /// <summary>
    /// 追加したすべての作業記録を取得します。
    /// </summary>
    /// <returns>追加した作業記録。</returns>
    public IReadOnlyList<TimeRecord> All => timeRecords;
}
