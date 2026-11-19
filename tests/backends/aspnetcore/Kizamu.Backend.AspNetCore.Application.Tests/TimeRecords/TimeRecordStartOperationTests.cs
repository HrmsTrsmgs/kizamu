using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class TimeRecordStartOperationTests
{
    readonly FakeTimeProvider timeProvider = new();
    readonly FakeTimeRecordState state = new();
    readonly TimeRecordStartOperation tested;

    public TimeRecordStartOperationTests() => tested = new(timeProvider, state);

    public static TheoryData<DateTimeOffset> 開始時刻の例 =>
    [
        new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 8, 1, 10, 30, 0, TimeSpan.Zero),
    ];

    [Theory]
    [MemberData(nameof(開始時刻の例))]
    public void 記録を開始すると空でない識別子と開始時点の現在時刻が返ります(DateTimeOffset startedAt)
    {
        timeProvider.SetUtcNow(startedAt);

        var result = tested.Execute();

        result.Id.Should().NotBeEmpty();
        result.StartedAt.Should().Be(startedAt);
    }

    [Theory]
    [MemberData(nameof(開始時刻の例))]
    public void 記録を開始すると開始結果の識別子と現在時刻を持つ終了時刻未設定の記録が保存されます(DateTimeOffset startedAt)
    {
        timeProvider.SetUtcNow(startedAt);

        var result = tested.Execute();

        state.Current.Should().NotBeNull();
        state.Current.Id.Should().Be(result.Id);
        state.Current.StartedAt.Should().Be(startedAt);
        state.Current.EndedAt.Should().BeNull();
    }
}
