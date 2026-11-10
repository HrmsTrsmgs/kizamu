using System.Collections.Immutable;
using Kizamu.Domain;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.TestSupport;

/// <summary>画面とViewModelのテストで、通信なしに確定済みのDomainの記録を返します。</summary>
/// <param name="startedRecord">開始操作で返す記録。</param>
/// <param name="endedRecord">終了操作で返す記録。</param>
sealed class StubTimeRecordService(TimeRecord startedRecord, TimeRecord endedRecord) : ITimeRecordService
{
    TaskCompletionSource<ImmutableArray<TimeRecord>>? listResponse;

    public ImmutableArray<TimeRecord> Records { get; set; } = [];
    public TimeRecord StartedRecord { get; set; } = startedRecord;
    public TimeRecord EndedRecord { get; } = endedRecord;
    public List<Guid> EndedIds { get; } = [];

    /// <summary>一覧の応答を、呼び出し側が完了させるまで保留します。</summary>
    public TaskCompletionSource<ImmutableArray<TimeRecord>> DelayListResponse()
    {
        listResponse = new();
        return listResponse;
    }

    public Task<ImmutableArray<TimeRecord>> GetTimeRecordsAsync() => listResponse?.Task ?? Task.FromResult(Records);

    public Task<TimeRecord> StartTimeRecordAsync() => Task.FromResult(StartedRecord);

    public Task<TimeRecord> EndTimeRecordAsync(Guid id)
    {
        EndedIds.Add(id);
        return Task.FromResult(EndedRecord);
    }
}
