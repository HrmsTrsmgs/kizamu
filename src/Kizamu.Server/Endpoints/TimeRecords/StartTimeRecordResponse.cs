namespace Kizamu.Server.Endpoints.TimeRecords;

sealed record StartTimeRecordResponse(Guid Id, DateTimeOffset StartedAt);
