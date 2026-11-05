namespace Kizamu.Backend.AspNetCore.Application.TimeRecords;

/// <summary>
/// 保存されている作業記録を一覧用の読取結果へ変換します。
/// </summary>
/// <param name="timeRecordState">作業記録の状態取得元。</param>
public sealed class GetTimeRecords(ITimeRecordState timeRecordState)
{
    /// <summary>
    /// 保存順を保ち、作業記録の識別子と開始・終了時刻を取得します。
    /// </summary>
    /// <returns>保存されている作業記録の読取結果。記録中の終了時刻はnullです。</returns>
    public IEnumerable<GetTimeRecordResult> Execute() =>
        timeRecordState.All.Select(it => new GetTimeRecordResult(it.Id, it.StartedAt, it.EndedAt));
}

/// <summary>
/// 一覧で取得した作業記録を表します。
/// </summary>
/// <param name="Id">作業記録の識別子。</param>
/// <param name="StartedAt">作業記録を開始した時刻。</param>
/// <param name="EndedAt">作業記録を終了した時刻。記録中の場合はnull。</param>
public sealed record GetTimeRecordResult(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);
