using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class TimeRecordReaderTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 次の開始時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
    readonly FakeTimeRecordState state;
    readonly TimeRecordReader tested;
    readonly TimeRecord 最初の記録;
    readonly TimeRecord 次の記録;

    public TimeRecordReaderTests()
    {
        state = new();
        tested = new(state);
        最初の記録 = TimeRecord.Start(開始時刻);
        次の記録 = TimeRecord.Start(次の開始時刻);
    }

    [Fact]
    public void 保存した記録がなければ記録一覧は空です()
    {
        tested.Execute().Should().BeEmpty();
    }

    [Fact]
    public void 記録一覧では記録中と終了済みの各記録の識別子と時刻を保存順に取得できます()
    {
        var 終了時刻 = 次の開始時刻.AddHours(1);
        state.Add(次の記録.End(終了時刻));
        state.Add(最初の記録);

        tested.Execute().Should().Equal(
            new GetTimeRecordResult(次の記録.Id, 次の開始時刻, 終了時刻),
            new GetTimeRecordResult(最初の記録.Id, 開始時刻, null));
    }

    [Fact]
    public void 同じ記録一覧を列挙するたびにその時点までに追加した記録を取得できます()
    {
        var 一覧 = tested.Execute();
        state.Add(最初の記録);

        一覧.Should().Equal(new GetTimeRecordResult(最初の記録.Id, 開始時刻, null));

        state.Add(次の記録);

        一覧.Should().Equal(
            new GetTimeRecordResult(最初の記録.Id, 開始時刻, null),
            new GetTimeRecordResult(次の記録.Id, 次の開始時刻, null));
    }

    [Fact]
    public void 記録一覧の列挙中に追加した記録はその回の列挙に含まれません()
    {
        state.Add(最初の記録);
        var 一覧 = tested.Execute();
        using var 列挙子 = 一覧.GetEnumerator();

        列挙子.MoveNext().Should().BeTrue();
        列挙子.Current.Should().Be(new GetTimeRecordResult(最初の記録.Id, 開始時刻, null));
        state.Add(次の記録);

        列挙子.MoveNext().Should().BeFalse();
    }
}
