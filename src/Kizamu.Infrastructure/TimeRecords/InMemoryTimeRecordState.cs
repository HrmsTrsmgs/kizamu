using Kizamu.Application.TimeRecords;

namespace Kizamu.Infrastructure.TimeRecords;

/// <summary>
/// インスタンスの存続中、作業記録の状態をメモリ上に保持します。
/// </summary>
public sealed class InMemoryTimeRecordState : ITimeRecordState
{
    /// <inheritdoc />
    public DateTimeOffset StartedAt { get; set; }
}
