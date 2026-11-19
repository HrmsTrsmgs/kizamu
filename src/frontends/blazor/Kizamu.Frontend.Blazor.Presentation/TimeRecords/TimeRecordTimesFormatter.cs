using System.Globalization;

namespace Kizamu.Frontend.Blazor.Presentation.TimeRecords;

/// <summary>記録の時刻と経過時間を画面用の文字列に整えます。</summary>
public static class TimeRecordTimesFormatter
{
    /// <summary>記録の時刻を日本時間の年月日時分で表します。</summary>
    public static string FormatDateTime(DateTimeOffset instant) =>
        instant.ToOffset(TimeSpan.FromHours(9)).ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

    /// <summary>経過時間を通算の時間と分で表します。</summary>
    public static string FormatDuration(TimeSpan elapsed) => $"{(long)elapsed.TotalHours}時間{elapsed.Minutes}分";
}
