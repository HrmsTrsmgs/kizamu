using AwesomeAssertions;
using Kizamu.Frontend.Blazor.Presentation.TimeRecords;

namespace Kizamu.Frontend.Blazor.Presentation.Tests.TimeRecords;

public sealed class TimeRecordTimesFormatterTests
{
    static readonly DateTimeOffset 開始時刻 = DateTimeOffset.Parse("2026-08-01T09:00:00Z");

    [Fact]
    public void 開始時刻と終了時刻を日本時間の年月日時分で表示します()
    {
        TimeRecordTimesFormatter.FormatDateTime(開始時刻).Should().Be("2026/08/01 18:00");
        TimeRecordTimesFormatter.FormatDateTime(開始時刻.AddMinutes(90)).Should().Be("2026/08/01 19:30");
    }

    [Theory]
    [InlineData(90, "1時間30分")]
    [InlineData(135, "2時間15分")]
    public void 終了済みの作業時間は開始から終了までの時間分で表示します(int 経過分, string 表示)
    {
        TimeRecordTimesFormatter.FormatDuration(開始時刻, 開始時刻.AddMinutes(経過分),
            () => throw new InvalidOperationException("終了済みの記録には現在時刻を使いません。")).Should().Be(表示);
    }

    [Theory]
    [InlineData(90, "1時間30分")]
    [InlineData(135, "2時間15分")]
    public void 記録中の作業時間は開始から現在までの時間分で表示します(int 経過分, string 表示)
    {
        TimeRecordTimesFormatter.FormatDuration(開始時刻, null, () => 開始時刻.AddMinutes(経過分)).Should().Be(表示);
    }
}
