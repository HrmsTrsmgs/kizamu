namespace Kizamu.Blazor.Client.TimeRecords;

/// <summary>HTTP APIで交換する作業記録です。業務ルールを持つDomainとは区別します。</summary>
/// <param name="Id">サーバーが発行した記録の識別子。</param>
/// <param name="StartedAt">記録開始の瞬間。</param>
/// <param name="EndedAt">記録終了の瞬間。記録中はnullです。</param>
public readonly record struct TimeRecordResponse(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? EndedAt);
