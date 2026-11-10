using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

/// <summary>
/// インスタンスの存続中、作業記録の状態をメモリ上に保持します。
/// </summary>
public sealed class InMemoryTimeRecordState : ITimeRecordState
{
    readonly List<TimeRecord> timeRecords = [];
    readonly Lock sync = new();

    /// <inheritdoc />
    public TimeRecord? Current
    {
        get
        {
            lock (sync)
                return timeRecords.LastOrDefault();
        }
    }

    /// <inheritdoc />
    public TimeRecord? Find(Guid id)
    {
        lock (sync)
            return timeRecords.FirstOrDefault(it => it.Id == id);
    }

    /// <inheritdoc />
    public TimeRecord[] CopyAll()
    {
        lock (sync)
            return [.. timeRecords];
    }

    /// <summary>
    /// 作業記録を追加します。
    /// </summary>
    /// <param name="timeRecord">追加する作業記録。</param>
    public void Add(TimeRecord timeRecord)
    {
        lock (sync)
            timeRecords.Add(timeRecord);
    }

    /// <summary>
    /// 追加したすべての作業記録を取得します。
    /// </summary>
    /// <returns>追加した作業記録。</returns>
    public IReadOnlyList<TimeRecord> All => timeRecords;
}
