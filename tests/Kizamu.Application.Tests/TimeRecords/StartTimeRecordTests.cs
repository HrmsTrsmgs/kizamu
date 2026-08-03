using FluentAssertions;
using Kizamu.Application.TimeRecords;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Application.Tests.TimeRecords;

public sealed class StartTimeRecordTests
{
    static readonly DateTimeOffset 現在時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void 記録を開始すると空でない識別子が返されます()
    {
        var startTimeRecord =
            new StartTimeRecord(
                new FakeTimeProvider(現在時刻),
                new FakeTimeRecordState());

        var result = startTimeRecord.Execute();

        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void 記録を開始すると現在時刻が開始時刻として返されます()
    {
        var startTimeRecord =
            new StartTimeRecord(
                new FakeTimeProvider(現在時刻),
                new FakeTimeRecordState());

        var result = startTimeRecord.Execute();

        result.StartedAt.Should().Be(現在時刻);
    }

    [Fact]
    public void 記録を開始すると開始時刻が保存されます()
    {
        var timeRecordState = new FakeTimeRecordState();
        var startTimeRecord =
            new StartTimeRecord(
                new FakeTimeProvider(現在時刻),
                timeRecordState);

        startTimeRecord.Execute();

        timeRecordState.StartedAt.Should().Be(現在時刻);
    }

    sealed class FakeTimeRecordState : ITimeRecordState
    {
        public DateTimeOffset StartedAt { get; set; }
    }
}
