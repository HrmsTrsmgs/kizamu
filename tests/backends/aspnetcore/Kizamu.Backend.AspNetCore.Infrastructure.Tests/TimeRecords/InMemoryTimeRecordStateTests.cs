using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests.TimeRecords;

public sealed class InMemoryTimeRecordStateTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 次の開始時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void 作業記録を保存すると同じ記録を取得できます()
    {
        var timeRecord = TimeRecord.Start(開始時刻);
        var tested = new InMemoryTimeRecordState();

        tested.Add(timeRecord);

        tested.Current.Should().BeSameAs(timeRecord);
    }

    [Fact]
    public void 複数の記録から指定した識別子の記録を取得できる()
    {
        var 最初の記録 = TimeRecord.Start(開始時刻);
        var 次の記録 = TimeRecord.Start(次の開始時刻);
        var tested = new InMemoryTimeRecordState();
        tested.Add(最初の記録);
        tested.Add(次の記録);

        tested.Find(最初の記録.Id).Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void 作業記録を追加していない場合は空の一覧を取得できます()
    {
        var tested = new InMemoryTimeRecordState();

        tested.All.Should().BeEmpty();
    }

    [Fact]
    public void 作業記録を一件追加すると追加した記録を取得できます()
    {
        var 追加する記録 = TimeRecord.Start(開始時刻);
        var tested = new InMemoryTimeRecordState();

        tested.Add(追加する記録);

        tested.All.Should().Equal(追加する記録);
    }

    [Fact]
    public void 作業記録を二件追加すると追加した順に両方取得できます()
    {
        var 最初の記録 = TimeRecord.Start(開始時刻);
        var 次の記録 = TimeRecord.Start(次の開始時刻);
        var tested = new InMemoryTimeRecordState();

        tested.Add(最初の記録);
        tested.Add(次の記録);

        tested.All.Should().Equal(最初の記録, 次の記録);
    }

    [Fact]
    public void 一覧構成のコピーは後から追加されず記録自体は共有します()
    {
        var 最初の記録 = TimeRecord.Start(開始時刻);
        var 次の記録 = TimeRecord.Start(次の開始時刻);
        var tested = new InMemoryTimeRecordState();
        tested.Add(最初の記録);
        var 一覧 = tested.All;

        var コピー = tested.CopyAll();
        tested.Add(次の記録);

        コピー.Should().ContainSingle().Which.Should().BeSameAs(最初の記録);
        一覧.Should().Equal(最初の記録, 次の記録);
    }
    [Fact]
    public void 同じ一覧を列挙するたびに列挙開始前に追加された記録を取得できます()
    {
        var tested = new InMemoryTimeRecordState();
        var 一覧 = tested.All;
        var 最初の記録 = TimeRecord.Start(開始時刻);
        tested.Add(最初の記録);

        一覧.Should().Equal(最初の記録);

        var 次の記録 = TimeRecord.Start(次の開始時刻);
        tested.Add(次の記録);

        一覧.Should().Equal(最初の記録, 次の記録);
        一覧.Count.Should().Be(2);
        一覧[0].Should().BeSameAs(最初の記録);
        一覧[1].Should().BeSameAs(次の記録);
    }
}
