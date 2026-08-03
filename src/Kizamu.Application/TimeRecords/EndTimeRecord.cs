namespace Kizamu.Application.TimeRecords;

public sealed class EndTimeRecord(TimeProvider timeProvider)
{
    public EndTimeRecordResult Execute(Guid id, DateTimeOffset startedAt) =>
        new(id, startedAt, timeProvider.GetUtcNow());
}

public sealed record EndTimeRecordResult(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt);
