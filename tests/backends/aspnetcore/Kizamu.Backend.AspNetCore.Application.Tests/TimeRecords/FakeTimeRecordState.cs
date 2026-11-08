using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

sealed class FakeTimeRecordState : ITimeRecordState
{
    readonly List<TimeRecord> timeRecords = [];

    public TimeRecord? Current => timeRecords.LastOrDefault();

    public IReadOnlyList<TimeRecord> All => timeRecords;

    public TimeRecord? Find(Guid id) => timeRecords.SingleOrDefault(it => it.Id == id);

    public void Add(TimeRecord timeRecord)
    {
        timeRecords.Add(timeRecord);
    }
}
