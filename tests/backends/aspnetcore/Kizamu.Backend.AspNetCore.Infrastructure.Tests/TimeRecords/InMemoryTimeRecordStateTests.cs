using AwesomeAssertions;
using Kizamu.Domain;
using Kizamu.Backend.AspNetCore.Infrastructure.TimeRecords;

namespace Kizamu.Backend.AspNetCore.Infrastructure.Tests.TimeRecords;

public sealed class InMemoryTimeRecordStateTests
{
    static readonly DateTimeOffset 開始時刻 = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset 次の開始時刻 = new(2026, 8, 1, 10, 30, 0, TimeSpan.Zero);
    readonly InMemoryTimeRecordState tested = new();
    readonly TimeRecord 最初の記録 = TimeRecord.Start(開始時刻);
    readonly TimeRecord 次の記録 = TimeRecord.Start(次の開始時刻);

    [Fact]
    public void 作業記録を保存すると同じ記録を取得できます()
    {
        tested.Add(最初の記録);

        tested.Current.Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void 複数の記録から指定した識別子の記録を取得できる()
    {
        tested.Add(最初の記録);
        tested.Add(次の記録);

        tested.Find(最初の記録.Id).Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void 作業記録を追加していない場合は空の一覧を取得できます()
    {
        tested.All.Should().BeEmpty();
    }

    [Fact]
    public void 作業記録を一件追加すると追加した記録を取得できます()
    {
        tested.Add(最初の記録);

        tested.All.Should().Equal(最初の記録);
    }

    [Fact]
    public void 作業記録を二件追加すると追加した順に両方取得できます()
    {
        tested.Add(最初の記録);
        tested.Add(次の記録);

        tested.All.Should().Equal(最初の記録, 次の記録);
    }

    [Fact]
    public void 終了した記録で更新すると一覧の元の位置に反映される()
    {
        tested.Add(最初の記録);
        tested.Add(次の記録);
        var 終了した記録 = 最初の記録.End(次の開始時刻);

        tested.Update(終了した記録);

        tested.All.Should().Equal(終了した記録, 次の記録);
    }

    [Fact]
    public void コピー後に追加した記録はコピーした一覧に含まれません()
    {
        tested.Add(最初の記録);

        var コピー = tested.CopyAll();
        tested.Add(次の記録);

        コピー.Select(it => it.Id).Should().Equal(最初の記録.Id);
    }

    [Fact]
    public void コピーした一覧から元と同じ記録インスタンスを取得できます()
    {
        tested.Add(最初の記録);

        var コピー = tested.CopyAll();
        tested.Add(次の記録);

        コピー[0].Should().BeSameAs(最初の記録);
    }

    [Fact]
    public void コピー後も元の一覧には追加した記録が反映されます()
    {
        tested.Add(最初の記録);
        var 一覧 = tested.All;

        tested.CopyAll();
        tested.Add(次の記録);

        一覧.Should().Equal(最初の記録, 次の記録);
    }

    [Fact]
    public void 同じ一覧を列挙するたびに列挙開始前に追加された記録を取得できます()
    {
        var 一覧 = tested.All;
        tested.Add(最初の記録);

        一覧.Should().Equal(最初の記録);

        tested.Add(次の記録);

        一覧.Should().Equal(最初の記録, 次の記録);
    }

    [Fact]
    public void 取得済みの一覧の件数に後から追加した記録が反映されます()
    {
        var 一覧 = tested.All;

        tested.Add(最初の記録);
        tested.Add(次の記録);

        一覧.Count.Should().Be(2);
    }

    [Fact]
    public void 取得済みの一覧の添字で後から追加した記録を追加順に取得できます()
    {
        var 一覧 = tested.All;

        tested.Add(最初の記録);
        tested.Add(次の記録);

        一覧[0].Should().BeSameAs(最初の記録);
        一覧[1].Should().BeSameAs(次の記録);
    }
}
