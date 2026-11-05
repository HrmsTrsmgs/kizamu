using Kizamu.Domain;

namespace Kizamu.Frontend.Blazor.Infrastructure.TimeRecords;

/// <summary>HTTP APIで交換する作業記録です。業務ルールを持つDomainとは区別します。</summary>
/// <param name="Id">サーバーが発行した記録の識別子。</param>
/// <param name="StartedAt">記録開始の瞬間。</param>
/// <param name="EndedAt">記録終了の瞬間。記録中はnullです。</param>
public readonly record struct TimeRecordResponse(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? EndedAt)
{
    /// <summary>通信形式から、画面が扱う作業記録へ変換します。</summary>
    internal TimeRecord ToDomain() => new(Id, StartedAt, EndedAt);
}
