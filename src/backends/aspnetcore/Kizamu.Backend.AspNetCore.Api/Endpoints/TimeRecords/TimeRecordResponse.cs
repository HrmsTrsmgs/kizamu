namespace Kizamu.Backend.AspNetCore.Api.Endpoints.TimeRecords;

/// <summary>
/// APIが返す作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
/// <param name="EndedAt">作業記録を終了した時刻。記録中の場合はnull。</param>
sealed record TimeRecordResponse(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);
