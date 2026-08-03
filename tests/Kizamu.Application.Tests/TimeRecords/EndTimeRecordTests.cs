using FluentAssertions;
using Kizamu.Application.TimeRecords;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Application.Tests.TimeRecords;

public sealed class EndTimeRecordTests
{
    static readonly Guid 識別子 = new("019cde5b-4c00-7000-8000-000000000001");
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 現在時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void 記録を終了すると指定した識別子が返されます()
    {
        var endTimeRecord =
            new EndTimeRecord(
                new FakeTimeProvider(現在時刻));

        var result = endTimeRecord.Execute(識別子, 開始時刻);

        result.Id.Should().Be(識別子);
    }

    [Fact]
    public void 記録を終了すると指定した開始時刻が返されます()
    {
        var endTimeRecord =
            new EndTimeRecord(
                new FakeTimeProvider(現在時刻));

        var result = endTimeRecord.Execute(識別子, 開始時刻);

        result.StartedAt.Should().Be(開始時刻);
    }

    [Fact]
    public void 記録を終了すると現在時刻が終了時刻として返されます()
    {
        var endTimeRecord =
            new EndTimeRecord(
                new FakeTimeProvider(現在時刻));

        var result = endTimeRecord.Execute(識別子, 開始時刻);

        result.EndedAt.Should().Be(現在時刻);
    }
}
