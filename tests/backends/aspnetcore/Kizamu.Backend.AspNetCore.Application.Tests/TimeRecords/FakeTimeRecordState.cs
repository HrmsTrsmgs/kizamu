using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

sealed class FakeTimeRecordState : ITimeRecordState
{
    readonly List<TimeRecord> timeRecords = [];

    public TimeRecord? Current => timeRecords.LastOrDefault();

    public IReadOnlyList<TimeRecord> All => timeRecords;

    public void Add(TimeRecord timeRecord)
    {
        timeRecords.Add(timeRecord);
    }
}
