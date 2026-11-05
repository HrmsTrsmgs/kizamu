using AwesomeAssertions;
using Kizamu.Backend.AspNetCore.Application.TimeRecords;
using Kizamu.Domain;

namespace Kizamu.Backend.AspNetCore.Application.Tests.TimeRecords;

public sealed class GetTimeRecordsTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 終了時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void 保存された記録がなければ空の一覧を返す()
    {
        var tested = new GetTimeRecords(new FakeTimeRecordState());

        tested.Execute().Should().BeEmpty();
    }

    [Fact]
    public void 保存された順に記録中と終了済みの記録の識別子と時刻を返す()
    {
        var 記録中の記録 = TimeRecord.Start(開始時刻);
        var 終了済みの記録 = TimeRecord.Start(終了時刻);
        終了済みの記録.End(終了時刻.AddHours(1));
        var state = new FakeTimeRecordState();
        state.Add(記録中の記録);
        state.Add(終了済みの記録);
        var tested = new GetTimeRecords(state);

        tested.Execute().Should().Equal(
            new GetTimeRecordResult(記録中の記録.Id, 開始時刻, null),
            new GetTimeRecordResult(終了済みの記録.Id, 終了時刻, 終了時刻.AddHours(1)));
    }
}
