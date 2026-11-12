using System.Globalization;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録の時刻と経過時間を画面用の文字列に整えます。</summary>
public static class TimeRecordTimesFormatter
{
    /// <summary>記録の時刻を日本時間の年月日時分で表します。</summary>
    public static string FormatDateTime(DateTimeOffset instant) =>
        instant.ToOffset(TimeSpan.FromHours(9)).ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>終了済みの記録は終了まで、記録中は現在までの時間と分を表します。</summary>
    /// <param name="startedAt">記録の開始時刻。</param>
    /// <param name="endedAt">記録の終了時刻。記録中の場合はnull。</param>
    /// <param name="getUtcNow">記録中の場合にだけ読み取る現在時刻。</param>
    public static string FormatDuration(DateTimeOffset startedAt, DateTimeOffset? endedAt, Func<DateTimeOffset> getUtcNow)
    {
        var elapsed = (endedAt ?? getUtcNow()) - startedAt;
        return $"{(long)elapsed.TotalHours}時間{elapsed.Minutes}分";
    }
}
