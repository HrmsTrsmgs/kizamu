using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class StartTimeRecordTests
{
    public static TheoryData<DateTimeOffset> 開始時刻の例 =>
    [
        new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 1, 10, 30, 0, TimeSpan.Zero),
    ];

    [Theory]
    [MemberData(nameof(開始時刻の例))]
    public void 記録を開始すると識別子と現在の開始時刻を返し終了時刻のない記録を保存する(
        DateTimeOffset startedAt)
    {
        var state = new FakeTimeRecordState();
        var tested = new StartTimeRecord(new FakeTimeProvider(startedAt), state);

        var result = tested.Execute();

        result.Id.Should().NotBeEmpty();
        result.StartedAt.Should().Be(startedAt);
        state.Current.Should().NotBeNull();
        state.Current.Id.Should().Be(result.Id);
        state.Current.StartedAt.Should().Be(startedAt);
        state.Current.EndedAt.Should().BeNull();
    }
}
