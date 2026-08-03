namespace Kizamu.Server.Endpoints.TimeRecords;

/// <summary>
/// 記録開始APIが返す作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
sealed record StartTimeRecordResponse(Guid Id, DateTimeOffset StartedAt);
