using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;
using Microsoft.Extensions.Time.Testing;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class TimeRecordStartOperationTests
{
    static readonly DateTimeOffset 最初の開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 次の開始時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
    readonly FakeTimeProvider timeProvider = new();
    readonly FakeTimeRecordState state = new();
    readonly TimeRecordStartOperation tested;

    public TimeRecordStartOperationTests() => tested = new(timeProvider, state);

    [Fact]
    public void 記録を二回開始するとそれぞれの開始時点の現在時刻が返ります()
    {
        timeProvider.SetUtcNow(最初の開始時刻);
        tested.Execute().StartedAt.Should().Be(最初の開始時刻);

        timeProvider.SetUtcNow(次の開始時刻);
        tested.Execute().StartedAt.Should().Be(次の開始時刻);
    }

    [Fact]
    public void 開始結果の識別子と開始時刻を持つ終了時刻未設定の記録が保存されます()
    {
        timeProvider.SetUtcNow(最初の開始時刻);

        var result = tested.Execute();

        state.Current.Should().Be(new TimeRecord(result.Id, result.StartedAt, null));
    }
}
