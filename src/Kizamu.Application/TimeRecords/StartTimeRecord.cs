namespace Kizamu.Application.TimeRecords;

public sealed class StartTimeRecord(TimeProvider timeProvider)
{
    public StartTimeRecordResult Execute() =>
        new(Guid.NewGuid(), timeProvider.GetUtcNow());
}

public sealed record StartTimeRecordResult(Guid Id, DateTimeOffset StartedAt);
