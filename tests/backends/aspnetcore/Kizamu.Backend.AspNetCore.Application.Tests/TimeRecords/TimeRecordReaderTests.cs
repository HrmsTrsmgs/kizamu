using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class TimeRecordReaderTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 終了時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
    readonly FakeTimeRecordState state;
    readonly TimeRecordReader tested;

    public TimeRecordReaderTests()
    {
        state = new();
        tested = new(state);
    }

    [Fact]
    public void 保存された記録がなければ空の一覧を返す()
    {
        tested.Execute().Should().BeEmpty();
    }

    [Fact]
    public void 保存された順に記録中と終了済みの記録の識別子と時刻を返す()
    {
        var (記録中の記録, 終了済みの記録) = 記録中と終了済みの記録を保存する(開始時刻, 終了時刻, 終了時刻.AddHours(1));

        tested.Execute().Should().Equal(
            new GetTimeRecordResult(記録中の記録.Id, 開始時刻, null),
            new GetTimeRecordResult(終了済みの記録.Id, 終了時刻, 終了時刻.AddHours(1)));
    }

    [Fact]
    public void 一覧の各列挙開始前に追加された記録を返す()
    {
        var 一覧 = tested.Execute();
        var 最初の記録 = TimeRecord.Start(開始時刻);
        state.Add(最初の記録);

        一覧.Should().Equal(new GetTimeRecordResult(最初の記録.Id, 開始時刻, null));

        var 次の記録 = TimeRecord.Start(終了時刻);
        state.Add(次の記録);

        一覧.Should().Equal(
            new GetTimeRecordResult(最初の記録.Id, 開始時刻, null),
            new GetTimeRecordResult(次の記録.Id, 終了時刻, null));
    }

    [Fact]
    public void 一覧の列挙中に記録を追加しても列挙を完了できる()
    {
        var 最初の記録 = TimeRecord.Start(開始時刻);
        var 次の記録 = TimeRecord.Start(終了時刻);
        state.Add(最初の記録);
        var 一覧 = tested.Execute();
        using var 列挙子 = 一覧.GetEnumerator();

        列挙子.MoveNext().Should().BeTrue();
        列挙子.Current.Should().Be(new GetTimeRecordResult(最初の記録.Id, 開始時刻, null));
        state.Add(次の記録);

        列挙子.MoveNext().Should().BeFalse();
    }

    (TimeRecord 記録中の記録, TimeRecord 終了済みの記録) 記録中と終了済みの記録を保存する(
        DateTimeOffset 記録中の開始時刻, DateTimeOffset 終了済みの開始時刻, DateTimeOffset 終了済みの終了時刻)
    {
        var 記録中の記録 = TimeRecord.Start(記録中の開始時刻);
        var 終了済みの記録 = TimeRecord.Start(終了済みの開始時刻);
        終了済みの記録.End(終了済みの終了時刻);
        state.Add(記録中の記録);
        state.Add(終了済みの記録);
        return (記録中の記録, 終了済みの記録);
    }
}
