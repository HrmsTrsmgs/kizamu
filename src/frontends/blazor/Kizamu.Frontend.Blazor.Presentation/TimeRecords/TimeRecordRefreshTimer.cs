namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録中の作業時間を表示し直すタイミングを通知します。</summary>
/// <param name="clock">更新間隔を計る時計。</param>
/// <param name="refresh">作業時間の表示を更新する処理。</param>
public sealed class TimeRecordRefreshTimer(TimeProvider clock, Action refresh) : IDisposable
{
    static readonly TimeSpan interval = TimeSpan.FromSeconds(1);
    ITimer? timer;

    /// <summary>以前の定期更新を停止し、記録中であれば一秒後から一秒ごとに更新します。</summary>
    /// <param name="endedAt">記録の終了時刻。記録中の場合はnull。</param>
    public void ScheduleRefresh(DateTimeOffset? endedAt)
    {
        timer?.Dispose();
        timer = null;
        if (endedAt is null)
        {
            timer = clock.CreateTimer(_ => refresh(), null, interval, interval);
        }
    }

    /// <summary>作業時間の定期更新を停止します。</summary>
    public void Dispose() => timer?.Dispose();
}
